using ERP.Web.Models.Models;
using ERP.Web.Service.Service.Lession;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ERP.Web.Controllers.Lession
{
    /// <summary>
    /// 課程紀錄控制器
    /// 處理課程影片播放和進度記錄功能
    /// </summary>
    public class LessionController : Controller
    {
        private readonly LessionService _lessionService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public LessionController(LessionService lessionService, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _lessionService = lessionService;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// 課程列表頁面
        /// </summary>
        public async Task<IActionResult> Index(Guid? employeeMainID)
        {
            // TODO: 從 Session 或認證系統取得 EmployeeMainID
            // 目前先使用參數，實際應用時應從登入資訊取得
            if (!employeeMainID.HasValue)
            {
                // 預設值，實際應從 Session 取得
                employeeMainID = Guid.Parse("70D9291B-3D3B-481E-B33A-F4699685C9BB");
            }

            var trainingInfoList = await _lessionService.GetTrainingInfoListAsync();
            var progressDict = await _lessionService.GetTrainingProgressAsync(employeeMainID.Value);

            var viewModel = new LessionIndexViewModel
            {
                TrainingInfoList = trainingInfoList,
                ProgressDict = progressDict,
                EmployeeMainID = employeeMainID.Value
            };

            return View(viewModel);
        }

        /// <summary>
        /// 播放課程影片頁面（單一片段）
        /// </summary>
        public async Task<IActionResult> Play(Guid id, Guid? employeeMainID)
        {
            // TODO: 從 Session 或認證系統取得 EmployeeMainID
            if (!employeeMainID.HasValue)
            {
                employeeMainID = Guid.Parse("70D9291B-3D3B-481E-B33A-F4699685C9BB");
            }

            var trainingInfo = await _lessionService.GetTrainingInfoByIdAsync(id);
            if (trainingInfo == null)
            {
                return NotFound();
            }

            // 取得觀看進度
            var detl = await _lessionService.GetTrainingDetlAsync(employeeMainID.Value, id);

            var viewModel = new LessionPlayViewModel
            {
                TrainingInfo = trainingInfo,
                EmployeeMainID = employeeMainID.Value,
                WatchedTime = detl?.Time ?? TimeSpan.Zero,
                IsCompleted = detl?.State ?? false
            };

            return View(viewModel);
        }

        /// <summary>
        /// 多片段課程播放頁面（根據 Page 分組顯示所有片段）
        /// </summary>
        public async Task<IActionResult> PlayMultiSegment(int page, Guid? employeeMainID)
        {
            // TODO: 從 Session 或認證系統取得 EmployeeMainID
            if (!employeeMainID.HasValue)
            {
                employeeMainID = Guid.Parse("70D9291B-3D3B-481E-B33A-F4699685C9BB");
            }

            // 取得該分頁的所有訓練資訊（片段）
            var trainingInfoList = await _lessionService.GetTrainingInfoListAsync();
            var segments = trainingInfoList.Where(t => t.Page == page).OrderBy(t => t.Seq).ToList();

            if (!segments.Any())
            {
                return NotFound();
            }

            // 取得所有片段的觀看進度
            var progressDict = new Dictionary<Guid, EMTrainingDetl?>();
            foreach (var segment in segments)
            {
                var detl = await _lessionService.GetTrainingDetlAsync(employeeMainID.Value, segment.ID);
                progressDict[segment.ID] = detl;
            }

            var viewModel = new LessionMultiSegmentViewModel
            {
                Page = page,
                Segments = segments,
                ProgressDict = progressDict,
                EmployeeMainID = employeeMainID.Value
            };

            return View(viewModel);
        }

        /// <summary>
        /// 更新影片觀看進度（AJAX）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
        {
            // TODO: 從 Session 或認證系統取得 EmployeeMainID, CurrentUserID, CurrentDeptID
            var currentUserID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var currentDeptID = Guid.Parse("00000000-0000-0000-0000-000000000000");

            var result = await _lessionService.UpdateVideoProgressAsync(
                request.EmployeeMainID,
                request.TrainingInfoMainID,
                request.WatchedTime,
                request.IsCompleted,
                currentUserID,
                currentDeptID
            );

            return Json(new { success = result });
        }

        /// <summary>
        /// 儲存觀看時間（使用 FormData，相容舊版 API）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveTime(string time, string EmpID, string UrlTime, string VideoUrl)
        {
            // TODO: 從 Session 或認證系統取得 CurrentUserID, CurrentDeptID
            var currentUserID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var currentDeptID = Guid.Parse("00000000-0000-0000-0000-000000000000");

            if (!Guid.TryParse(EmpID, out Guid employeeMainID))
            {
                return Json(new { success = false, message = "無效的員工ID" });
            }

            // 根據 VideoUrl 找到對應的 TrainingInfo
            // 使用更寬鬆的比對方式，因為 URL 格式可能不同
            var trainingInfoList = await _lessionService.GetTrainingInfoListAsync();
            var trainingInfo = trainingInfoList.FirstOrDefault(t => 
                t.URL == VideoUrl || 
                t.URL.Contains(VideoUrl) || 
                VideoUrl.Contains(t.URL) ||
                ExtractVideoId(t.URL) == ExtractVideoId(VideoUrl));

            if (trainingInfo == null)
            {
                return Json(new { success = false, message = "找不到對應的訓練資訊" });
            }

            // 解析時間字串 (格式: HH:MM:SS)
            var timeParts = UrlTime.Split(':');
            if (timeParts.Length != 3)
            {
                return Json(new { success = false, message = "時間格式錯誤" });
            }

            var hours = int.Parse(timeParts[0]);
            var minutes = int.Parse(timeParts[1]);
            var seconds = int.Parse(timeParts[2]);
            var watchedTimeSeconds = hours * 3600 + minutes * 60 + seconds;

            // 判斷是否完成（如果觀看時間接近總時長）
            var isCompleted = watchedTimeSeconds >= (trainingInfo.URLTime.TotalSeconds * 0.95);

            var result = await _lessionService.UpdateVideoProgressAsync(
                employeeMainID,
                trainingInfo.ID,
                watchedTimeSeconds,
                isCompleted,
                currentUserID,
                currentDeptID
            );

            return Json(new { success = result });
        }

        /// <summary>
        /// 重置觀看進度（重看功能）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateDetl(Guid EMTrainingInfoMainID)
        {
            // TODO: 從 Session 或認證系統取得 EmployeeMainID, CurrentUserID, CurrentDeptID
            var employeeMainID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var currentUserID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var currentDeptID = Guid.Parse("00000000-0000-0000-0000-000000000000");

            // 重置觀看時間為 0，完成狀態為 false
            var result = await _lessionService.UpdateVideoProgressAsync(
                employeeMainID,
                EMTrainingInfoMainID,
                0,
                false,
                currentUserID,
                currentDeptID
            );

            if (result)
            {
                return Json(new { IsSuccess = true, Msg = "已重置觀看進度" });
            }
            else
            {
                return Json(new { IsSuccess = false, Msg = "重置失敗" });
            }
        }

        /// <summary>
        /// 新增課程頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // 取得最大 Page 編號，預設為下一個
            var maxPage = await _lessionService.GetMaxPageAsync();
            var viewModel = new LessionCreateViewModel
            {
                Page = maxPage + 1,
                MyModal = Guid.NewGuid().ToString("N").Substring(0, 8) // 產生唯一識別碼
            };

            return View(viewModel);
        }

        /// <summary>
        /// 新增課程（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LessionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: 從 Session 或認證系統取得 CurrentUserID, CurrentDeptID
            var currentUserID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var currentDeptID = Guid.Parse("00000000-0000-0000-0000-000000000000");

            // 解析時間字串 (格式: HH:MM:SS 或 MM:SS)
            TimeSpan urlTime;
            if (!TryParseTimeSpan(model.URLTimeString, out urlTime))
            {
                ModelState.AddModelError("URLTimeString", "時間格式錯誤，請使用 HH:MM:SS 或 MM:SS 格式");
                return View(model);
            }

            var trainingInfo = new EMTrainingInfo
            {
                MyModal = model.MyModal,
                Title = model.Title,
                Content = model.Content ?? string.Empty,
                URL = model.URL,
                URLTime = urlTime,
                URLStyle = model.URLStyle ?? string.Empty,
                TitleStyle = model.TitleStyle ?? string.Empty,
                ContentStyle = model.ContentStyle ?? string.Empty,
                Img = model.Img,
                ImgStyle = model.ImgStyle ?? string.Empty,
                Page = model.Page,
                CreateUser = currentUserID,
                CreateDept = currentDeptID,
                ModifyUser = currentUserID,
                ModifyDept = currentDeptID
            };

            var result = await _lessionService.CreateTrainingInfoAsync(trainingInfo);

            if (result)
            {
                TempData["SuccessMessage"] = "課程新增成功！";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "新增失敗，請稍後再試");
                return View(model);
            }
        }

        /// <summary>
        /// 從 YouTube URL 取得影片資訊（API endpoint）
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetYouTubeVideoInfo([FromBody] GetYouTubeVideoInfoRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Url))
                {
                    return Json(new { success = false, message = "請提供 YouTube URL" });
                }

                var videoId = ExtractVideoId(request.Url);
                if (string.IsNullOrEmpty(videoId))
                {
                    return Json(new { success = false, message = "無法識別 YouTube URL 格式" });
                }

                // 取得 YouTube API Key
                var apiKey = _configuration["YouTube:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Json(new { success = false, message = "YouTube API Key 未設定，請手動輸入影片時長" });
                }

                // 呼叫 YouTube Data API v3
                var apiUrl = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={apiKey}&part=contentDetails";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = $"無法取得影片資訊：{response.StatusCode}" });
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonContent);

                // 檢查是否有找到影片
                if (!jsonDoc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                {
                    return Json(new { success = false, message = "找不到該影片，請確認 URL 是否正確" });
                }

                // 取得影片長度（ISO 8601 格式，例如：PT1H30M45S）
                var contentDetails = items[0].GetProperty("contentDetails");
                var duration = contentDetails.GetProperty("duration").GetString();

                if (string.IsNullOrEmpty(duration))
                {
                    return Json(new { success = false, message = "無法取得影片長度資訊" });
                }

                // 將 ISO 8601 格式轉換為秒數
                var totalSeconds = ParseISO8601Duration(duration);
                if (totalSeconds <= 0)
                {
                    return Json(new { success = false, message = "無法解析影片長度" });
                }

                // 轉換為 HH:MM:SS 或 MM:SS 格式
                var timeString = FormatTimeSpan(totalSeconds);

                return Json(new
                {
                    success = true,
                    duration = timeString,
                    totalSeconds = totalSeconds
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"發生錯誤：{ex.Message}" });
            }
        }

        /// <summary>
        /// 解析 ISO 8601 時間格式（例如：PT1H30M45S）為秒數
        /// </summary>
        private int ParseISO8601Duration(string duration)
        {
            // ISO 8601 格式：PT1H30M45S (1小時30分45秒)
            // 移除 PT 前綴
            if (!duration.StartsWith("PT"))
                return 0;

            duration = duration.Substring(2);
            int totalSeconds = 0;

            // 解析小時
            var hourIndex = duration.IndexOf('H');
            if (hourIndex != -1)
            {
                if (int.TryParse(duration.Substring(0, hourIndex), out int hours))
                {
                    totalSeconds += hours * 3600;
                }
                duration = duration.Substring(hourIndex + 1);
            }

            // 解析分鐘
            var minuteIndex = duration.IndexOf('M');
            if (minuteIndex != -1)
            {
                if (int.TryParse(duration.Substring(0, minuteIndex), out int minutes))
                {
                    totalSeconds += minutes * 60;
                }
                duration = duration.Substring(minuteIndex + 1);
            }

            // 解析秒數
            var secondIndex = duration.IndexOf('S');
            if (secondIndex != -1)
            {
                if (int.TryParse(duration.Substring(0, secondIndex), out int seconds))
                {
                    totalSeconds += seconds;
                }
            }

            return totalSeconds;
        }

        /// <summary>
        /// 將秒數轉換為時間字串格式（HH:MM:SS 或 MM:SS）
        /// </summary>
        private string FormatTimeSpan(int totalSeconds)
        {
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            // 如果超過 1 小時，使用 HH:MM:SS 格式
            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            // 否則使用 MM:SS 格式
            else
            {
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 從 YouTube URL 中提取 Video ID
        /// </summary>
        private string ExtractVideoId(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (url.Contains("youtube.com/watch?v="))
            {
                var startIndex = url.IndexOf("watch?v=") + 8;
                var endIndex = url.IndexOf("&", startIndex);
                if (endIndex == -1) endIndex = url.Length;
                return url.Substring(startIndex, endIndex - startIndex);
            }
            else if (url.Contains("youtu.be/"))
            {
                var startIndex = url.IndexOf("youtu.be/") + 9;
                var endIndex = url.IndexOf("?", startIndex);
                if (endIndex == -1) endIndex = url.Length;
                return url.Substring(startIndex, endIndex - startIndex);
            }
            else if (url.Contains("youtube.com/embed/"))
            {
                var startIndex = url.IndexOf("embed/") + 6;
                var endIndex = url.IndexOf("?", startIndex);
                if (endIndex == -1) endIndex = url.Length;
                return url.Substring(startIndex, endIndex - startIndex);
            }

            return string.Empty;
        }

        /// <summary>
        /// 解析時間字串為 TimeSpan
        /// </summary>
        private bool TryParseTimeSpan(string timeString, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(timeString))
                return false;

            // 支援多種格式：HH:MM:SS, MM:SS, 或純秒數
            var parts = timeString.Split(':');
            if (parts.Length == 3)
            {
                // HH:MM:SS
                if (int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes) &&
                    int.TryParse(parts[2], out int seconds))
                {
                    result = new TimeSpan(hours, minutes, seconds);
                    return true;
                }
            }
            else if (parts.Length == 2)
            {
                // MM:SS
                if (int.TryParse(parts[0], out int minutes) &&
                    int.TryParse(parts[1], out int seconds))
                {
                    result = new TimeSpan(0, minutes, seconds);
                    return true;
                }
            }
            else if (parts.Length == 1)
            {
                // 純秒數
                if (int.TryParse(parts[0], out int totalSeconds))
                {
                    result = TimeSpan.FromSeconds(totalSeconds);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 課程列表 ViewModel
    /// </summary>
    public class LessionIndexViewModel
    {
        public List<EMTrainingInfo> TrainingInfoList { get; set; } = new();
        public Dictionary<Guid, TrainingProgress> ProgressDict { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 課程播放 ViewModel
    /// </summary>
    public class LessionPlayViewModel
    {
        public EMTrainingInfo TrainingInfo { get; set; } = null!;
        public Guid EmployeeMainID { get; set; }
        public TimeSpan WatchedTime { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 更新進度請求
    /// </summary>
    public class UpdateProgressRequest
    {
        public Guid EmployeeMainID { get; set; }
        public Guid TrainingInfoMainID { get; set; }
        public int WatchedTime { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 多片段課程播放 ViewModel
    /// </summary>
    public class LessionMultiSegmentViewModel
    {
        public int Page { get; set; }
        public List<EMTrainingInfo> Segments { get; set; } = new();
        public Dictionary<Guid, EMTrainingDetl?> ProgressDict { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 新增課程 ViewModel
    /// </summary>
    public class LessionCreateViewModel
    {
        [Required(ErrorMessage = "片段識別碼為必填")]
        [Display(Name = "片段識別碼 (MyModal)")]
        public string MyModal { get; set; } = string.Empty;

        [Required(ErrorMessage = "標題為必填")]
        [Display(Name = "標題")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "內容描述")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "YouTube URL 為必填")]
        [Display(Name = "YouTube 影片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string URL { get; set; } = string.Empty;

        [Required(ErrorMessage = "影片時長為必填")]
        [Display(Name = "影片時長 (格式: HH:MM:SS 或 MM:SS)")]
        public string URLTimeString { get; set; } = string.Empty;

        [Display(Name = "URL 樣式 (CSS)")]
        public string? URLStyle { get; set; }

        [Display(Name = "標題樣式 (CSS)")]
        public string? TitleStyle { get; set; }

        [Display(Name = "內容樣式 (CSS)")]
        public string? ContentStyle { get; set; }

        [Display(Name = "圖片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string? Img { get; set; }

        [Display(Name = "圖片樣式 (CSS)")]
        public string? ImgStyle { get; set; }

        [Required(ErrorMessage = "分頁編號為必填")]
        [Display(Name = "分頁編號 (Page)")]
        [Range(1, int.MaxValue, ErrorMessage = "分頁編號必須大於 0")]
        public int Page { get; set; } = 1;
    }

    /// <summary>
    /// 取得 YouTube 影片資訊請求
    /// </summary>
    public class GetYouTubeVideoInfoRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}

