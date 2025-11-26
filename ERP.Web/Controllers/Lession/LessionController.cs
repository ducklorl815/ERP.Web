using ERP.Web.Models.Models.Lession;
using ERP.Web.Service.Service.Lession;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LessionMainProgress = ERP.Web.Service.Service.Lession.LessionMainProgress;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpAccessor;

        public LessionController(
            LessionService lessionService, 
            IConfiguration configuration, 
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _lessionService = lessionService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _httpAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 課程列表頁面（顯示 LessionMain 列表）
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var employeeMainID = GetEmployeeMainIDFromSession();

            // 取得所有 LessionMain 列表及其進度
            var lessionMainProgressDict = await _lessionService.GetLessionMainProgressAsync(employeeMainID);

            var viewModel = new LessionIndexViewModel
            {
                LessionMainProgressList = lessionMainProgressDict.Values.ToList(),
                EmployeeMainID = employeeMainID
            };

            return View(viewModel);
        }

        /// <summary>
        /// 播放課程影片頁面（單一片段）
        /// </summary>
        public async Task<IActionResult> Play(Guid id)
        {
            var employeeMainID = GetEmployeeMainIDFromSession();

            var trainingInfo = await _lessionService.GetTrainingInfoByIdAsync(id);
            if (trainingInfo == null)
            {
                return NotFound();
            }

            // 取得觀看進度
            var detl = await _lessionService.GetTrainingDetlAsync(employeeMainID, id);

            var viewModel = new LessionPlayViewModel
            {
                TrainingInfo = trainingInfo,
                EmployeeMainID = employeeMainID,
                WatchedTime = detl?.Time ?? 0, // 直接使用秒數（int）
                IsCompleted = detl?.State ?? false
            };

            return View(viewModel);
        }

        /// <summary>
        /// 多片段課程播放頁面（根據 LessionMain ID 顯示所有片段）
        /// </summary>
        public async Task<IActionResult> PlayMultiSegment(Guid lessionMainID)
        {
            try
            {
                var employeeMainID = GetEmployeeMainIDFromSession();
                
                if (employeeMainID == Guid.Empty)
                {
                    return BadRequest("無法取得用戶資訊，請重新登入");
                }

                // 取得 LessionMain
                var lessionMain = await _lessionService.GetLessionMainByIdAsync(lessionMainID);
                if (lessionMain == null)
                {
                    return NotFound();
                }

                // 根據 ParagraphJson 取得所有相關的 LessionInfo（片段）
                var segments = await _lessionService.GetTrainingInfoByLessionMainIdAsync(lessionMainID);
                
                if (!segments.Any())
                {
                    return NotFound();
                }

                // 取得所有片段的觀看進度
                var progressDict = new Dictionary<Guid, EMTrainingDetl?>();
                foreach (var segment in segments)
                {
                    try
                    {
                        var detl = await _lessionService.GetTrainingDetlAsync(employeeMainID, segment.ID);
                        progressDict[segment.ID] = detl;
                    }
                    catch
                    {
                        // 如果取得進度失敗，設為 null
                        progressDict[segment.ID] = null;
                    }
                }

                var viewModel = new LessionMultiSegmentViewModel
                {
                    LessionMainID = lessionMainID,
                    LessionMainName = lessionMain.LessionName ?? string.Empty,
                    Segments = segments,
                    ProgressDict = progressDict,
                    EmployeeMainID = employeeMainID
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // 記錄錯誤並返回錯誤頁面
                return StatusCode(500, $"發生錯誤：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新影片觀看進度（AJAX）
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateProgress([FromForm] string LessionInfoID, [FromForm] int WatchedTime = 0, [FromForm] bool IsCompleted = false)
        {
            try
            {
                // 驗證請求參數
                if (string.IsNullOrEmpty(LessionInfoID))
                {
                    return Json(new { IsSuccess = false, Msg = "請求參數為空" });
                }

                // 解析 GUID
                if (!Guid.TryParse(LessionInfoID, out Guid trainingInfoMainID) || 
                    trainingInfoMainID == Guid.Empty)
                {
                    return Json(new { IsSuccess = false, Msg = "無效的訓練資訊ID" });
                }

                // 從 Session 獲取用戶資訊
                Guid employeeMainID;
                Guid CreateUser;
                Guid CreateDept;
                
                try
                {
                    employeeMainID = GetEmployeeMainIDFromSession();
                    CreateUser = GetCreateUserFromSession();
                    CreateDept = GetCreateDeptFromSession();
                }
                catch (Exception sessionEx)
                {
                    return Json(new { IsSuccess = false, Msg = $"取得 Session 資訊失敗：{sessionEx.Message}" });
                }

                if (employeeMainID == Guid.Empty || CreateUser == Guid.Empty || CreateDept == Guid.Empty)
                {
                    return Json(new { IsSuccess = false, Msg = "無法取得用戶資訊，請重新登入" });
                }

                // 驗證觀看時間（必須 >= 0）
                if (WatchedTime < 0)
                {
                    WatchedTime = 0;
                }

                bool result;
                try
                {
                    result = await _lessionService.UpdateVideoProgressAsync(
                        employeeMainID,
                        trainingInfoMainID,
                        WatchedTime,
                        IsCompleted,
                        CreateUser,
                        CreateDept
                    );
                }
                catch (Exception serviceEx)
                {
                    return Json(new { IsSuccess = false, Msg = $"更新進度服務調用失敗：{serviceEx.Message}" });
                }

                if (result)
                {
                    return Json(new { IsSuccess = true, Msg = "進度已更新" });
                }
                else
                {
                    return Json(new { IsSuccess = false, Msg = "更新進度失敗" });
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤（實際應用中應該使用 ILogger）
                // 返回詳細錯誤信息以便調試
                var errorDetails = new
                {
                    IsSuccess = false,
                    Msg = $"更新進度時發生錯誤：{ex.Message}",
                    ExceptionType = ex.GetType().Name,
                    InnerException = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
                };
                
                // 在開發環境中返回詳細錯誤，生產環境中只返回簡化信息
                return Json(errorDetails);
            }
        }

        /// <summary>
        /// 儲存觀看時間（使用 FormData，相容舊版 API）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveTime(string time, string UrlTime, string VideoUrl)
        {
            // 從 Session 獲取用戶資訊
            var employeeMainID = GetEmployeeMainIDFromSession();
            var CreateUser = GetCreateUserFromSession();
            var CreateDept = GetCreateDeptFromSession();

            if (employeeMainID == Guid.Empty || CreateUser == Guid.Empty || CreateDept == Guid.Empty)
            {
                return Json(new { success = false, message = "無法取得用戶資訊" });
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
            var isCompleted = watchedTimeSeconds >= (trainingInfo.URLTime * 0.95);

            var result = await _lessionService.UpdateVideoProgressAsync(
                employeeMainID,
                trainingInfo.ID,
                watchedTimeSeconds,
                isCompleted,
                CreateUser,
                CreateDept
            );

            return Json(new { success = result });
        }

        /// <summary>
        /// 重置觀看進度（重看功能）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateDetl([FromForm] string LessionInfoID)
        {
            try
            {
                // 解析 GUID 參數
                if (!Guid.TryParse(LessionInfoID, out Guid trainingInfoMainID))
                {
                    return Json(new { IsSuccess = false, Msg = "無效的訓練資訊ID" });
                }

                // 從 Session 獲取用戶資訊
                var employeeMainID = GetEmployeeMainIDFromSession();
                var CreateUser = GetCreateUserFromSession();
                var CreateDept = GetCreateDeptFromSession();

                if (employeeMainID == Guid.Empty || CreateUser == Guid.Empty || CreateDept == Guid.Empty)
                {
                    return Json(new { IsSuccess = false, Msg = "無法取得用戶資訊" });
                }

                // 重置觀看時間為 0，完成狀態為 false
                var result = await _lessionService.UpdateVideoProgressAsync(
                    employeeMainID,
                    trainingInfoMainID,
                    0,
                    false,
                    CreateUser,
                    CreateDept
                );

                if (result)
                {
                    return Json(new { IsSuccess = true, Msg = "已重置觀看進度" });
                }
                else
                {
                    return Json(new { IsSuccess = false, Msg = "重置失敗：資料庫操作失敗" });
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤（實際應用中應該使用 ILogger）
                return Json(new { IsSuccess = false, Msg = $"重置失敗：{ex.Message}" });
            }
        }

        /// <summary>
        /// 新增課程頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // 取得所有 LessionMain 列表供選擇
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            var viewModel = new LessionCreateViewModel
            {
                LessionMainList = lessionMainList.Select(lm => new SelectListItem
                {
                    Value = lm.ID.ToString(),
                    Text = lm.LessionName
                }).ToList()
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
            // 重新載入 LessionMain 列表（用於驗證失敗時顯示）
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            model.LessionMainList = lessionMainList.Select(lm => new SelectListItem
            {
                Value = lm.ID.ToString(),
                Text = lm.LessionName
            }).ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 從 Session 獲取用戶資訊
            var CreateUser = GetCreateUserFromSession();
            var CreateDept = GetCreateDeptFromSession();



            if (CreateUser == Guid.Empty || CreateDept == Guid.Empty)
            {
                ModelState.AddModelError("", "無法取得用戶資訊");
                return View(model);
            }

            // 解析時間字串 (格式: HH:MM:SS 或 MM:SS) 轉換為秒數
            int urlTimeSeconds;
            if (!TryParseTimeToSeconds(model.URLTimeString, out urlTimeSeconds))
            {
                ModelState.AddModelError("URLTimeString", "時間格式錯誤，請使用 HH:MM:SS 或 MM:SS 格式");
                return View(model);
            }

            Guid lessionMainID;

            // 判斷是新增新課程還是選擇現有課程
            // 從表單傳來的值可能是字串 "true"/"false"，需要正確處理
            bool isNewLessionMain = model.IsNewLessionMain;
            if (Request.Form.ContainsKey("IsNewLessionMain"))
            {
                var formValue = Request.Form["IsNewLessionMain"].ToString();
                isNewLessionMain = formValue == "true";
            }

            if (isNewLessionMain)
            {
                // 新增新的 LessionMain
                if (string.IsNullOrWhiteSpace(model.NewLessionMainName))
                {
                    ModelState.AddModelError("NewLessionMainName", "新課程名稱為必填");
                    return View(model);
                }

                var newLessionMain = new LessionMain
                {
                    LessionName = model.NewLessionMainName,
                    ParagraphJson = string.Empty, // 初始為空，新增片段後會更新
                    ModifyUser = CreateUser,
                    ModifyDept = CreateDept
                };

                var createLessionMainID = await _lessionService.CreateLessionMainAsync(newLessionMain);
                if (createLessionMainID == Guid.Empty)
                {
                    ModelState.AddModelError("", "建立新課程失敗，請稍後再試");
                    return View(model);
                }

                lessionMainID = createLessionMainID;
            }
            else
            {
                // 使用現有的 LessionMain
                if (!model.LessionMainID.HasValue)
                {
                    ModelState.AddModelError("LessionMainID", "請選擇課程或建立新課程");
                    return View(model);
                }

                lessionMainID = model.LessionMainID.Value;
            }

            // 新增 LessionInfo（片段）
            var trainingInfo = new EMTrainingInfo
            {
                Title = model.Title,
                Content = model.Content ?? string.Empty,
                URL = model.URL,
                URLTime = urlTimeSeconds, // 使用秒數（int）
                Img = model.Img,
                Page = 0, // Page 不再使用，設為 0
                TagJson = model.TagJson,
                ModifyUser = CreateUser,
                ModifyDept = CreateDept
            };

            var createTrainingInfoGuid = await _lessionService.CreateTrainingInfoAsync(trainingInfo);
            if (createTrainingInfoGuid == Guid.Empty)
            {
                ModelState.AddModelError("", "新增片段失敗，請稍後再試");
                return View(model);
            }
            trainingInfo.ID = createTrainingInfoGuid;
            // 更新 LessionMain 的 ParagraphJson，將新的片段加入
            var lessionMain = await _lessionService.GetLessionMainByIdAsync(lessionMainID);
            if (lessionMain == null)
            {
                ModelState.AddModelError("", "找不到對應的課程，請重新選擇");
                return View(model);
            }

            // 解析現有的 ParagraphJson
            var paragraphs = _lessionService.ParseParagraphJson(lessionMain.ParagraphJson);
            
            // 新增新的片段到段落列表
            paragraphs.Add(new ParagraphInfo
            {
                LessionInfoID = trainingInfo.ID,
                Paragraph = model.Title // 使用標題作為段落描述
            });

            // 更新 ParagraphJson
            lessionMain.ParagraphJson = _lessionService.SerializeParagraphJson(paragraphs);
            lessionMain.ModifyUser = CreateUser;
            lessionMain.ModifyDept = CreateDept;

            var updateLessionMainResult = await _lessionService.UpdateLessionMainAsync(lessionMain);
            if (!updateLessionMainResult)
            {
                ModelState.AddModelError("", "更新課程資訊失敗，但片段已新增");
                return View(model);
            }

            TempData["SuccessMessage"] = "課程片段新增成功！";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 課程管理列表頁面（用於管理課程）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchList()
        {
            // 取得所有課程列表
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            
            // 計算每個課程的片段數量
            var lessionListWithSegmentCount = lessionMainList.Select(lessionMain =>
            {
                var paragraphs = _lessionService.ParseParagraphJson(lessionMain.ParagraphJson);
                return new LessionMainWithSegmentCount
                {
                    LessionMain = lessionMain,
                    SegmentCount = paragraphs?.Count ?? 0
                };
            }).ToList();
            
            var viewModel = new LessionSearchListViewModel
            {
                LessionListWithSegmentCount = lessionListWithSegmentCount
            };

            return View(viewModel);
        }

        /// <summary>
        /// 課程詳細資訊頁面（顯示課程的所有片段）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            // 取得課程資訊
            var lessionMain = await _lessionService.GetLessionMainByIdAsync(id);
            if (lessionMain == null)
            {
                return NotFound();
            }

            // 取得該課程的所有片段
            var segments = await _lessionService.GetTrainingInfoByLessionMainIdAsync(id);

            var viewModel = new LessionDetailsViewModel
            {
                LessionMain = lessionMain,
                Segments = segments
            };

            return View(viewModel);
        }

        /// <summary>
        /// 修改課程片段頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            // 取得要修改的訓練資訊
            var trainingInfo = await _lessionService.GetTrainingInfoByIdAsync(id);
            if (trainingInfo == null)
            {
                return NotFound();
            }

            // 找出該片段所屬的課程
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            Guid? currentLessionMainID = null;

            foreach (var lessionMain in lessionMainList)
            {
                var paragraphs = _lessionService.ParseParagraphJson(lessionMain.ParagraphJson);
                if (paragraphs.Any(p => p.LessionInfoID == id))
                {
                    currentLessionMainID = lessionMain.ID;
                    break;
                }
            }

            // 取得所有 LessionMain 列表供選擇
            var viewModel = new LessionEditViewModel
            {
                ID = trainingInfo.ID,
                Title = trainingInfo.Title,
                Content = trainingInfo.Content ?? string.Empty,
                URL = trainingInfo.URL,
                URLTimeString = FormatTimeSpan(trainingInfo.URLTime), // 將秒數轉換為時間字串
                Img = trainingInfo.Img ?? string.Empty,
                TagJson = trainingInfo.TagJson,
                LessionMainID = currentLessionMainID,
                LessionMainList = lessionMainList.Select(lm => new SelectListItem
                {
                    Value = lm.ID.ToString(),
                    Text = lm.LessionName,
                    Selected = lm.ID == currentLessionMainID
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// 修改課程片段（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LessionEditViewModel model)
        {
            // 重新載入 LessionMain 列表（用於驗證失敗時顯示）
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            model.LessionMainList = lessionMainList.Select(lm => new SelectListItem
            {
                Value = lm.ID.ToString(),
                Text = lm.LessionName,
                Selected = lm.ID == model.LessionMainID
            }).ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 取得要修改的訓練資訊
            var trainingInfo = await _lessionService.GetTrainingInfoByIdAsync(model.ID);
            if (trainingInfo == null)
            {
                return NotFound();
            }

            // 從 Session 獲取用戶資訊
            var CreateUser = GetCreateUserFromSession();
            var CreateDept = GetCreateDeptFromSession();

            if (CreateUser == Guid.Empty || CreateDept == Guid.Empty)
            {
                ModelState.AddModelError("", "無法取得用戶資訊");
                return View(model);
            }

            // 解析時間字串 (格式: HH:MM:SS 或 MM:SS) 轉換為秒數
            int urlTimeSeconds;
            if (!TryParseTimeToSeconds(model.URLTimeString, out urlTimeSeconds))
            {
                ModelState.AddModelError("URLTimeString", "時間格式錯誤，請使用 HH:MM:SS 或 MM:SS 格式");
                return View(model);
            }

            // 驗證課程選擇
            if (!model.LessionMainID.HasValue)
            {
                ModelState.AddModelError("LessionMainID", "請選擇課程");
                return View(model);
            }

            // 找出該片段原本所屬的課程
            Guid? oldLessionMainID = null;
            foreach (var lessionMain in lessionMainList)
            {
                var paragraphs = _lessionService.ParseParagraphJson(lessionMain.ParagraphJson);
                if (paragraphs.Any(p => p.LessionInfoID == model.ID))
                {
                    oldLessionMainID = lessionMain.ID;
                    break;
                }
            }

            // 更新訓練資訊
            trainingInfo.Title = model.Title;
            trainingInfo.Content = model.Content ?? string.Empty;
            trainingInfo.URL = model.URL;
            trainingInfo.URLTime = urlTimeSeconds;
            trainingInfo.Img = model.Img;
            trainingInfo.TagJson = model.TagJson;
            trainingInfo.ModifyUser = CreateUser;
            trainingInfo.ModifyDept = CreateDept;

            var updateResult = await _lessionService.UpdateTrainingInfoAsync(trainingInfo);
            if (!updateResult)
            {
                ModelState.AddModelError("", "更新片段失敗，請稍後再試");
                return View(model);
            }

            // 如果課程有變更，需要更新兩個課程的 ParagraphJson
            if (oldLessionMainID.HasValue && oldLessionMainID.Value != model.LessionMainID.Value)
            {
                // 從舊課程移除
                var oldLessionMain = await _lessionService.GetLessionMainByIdAsync(oldLessionMainID.Value);
                if (oldLessionMain != null)
                {
                    var oldParagraphs = _lessionService.ParseParagraphJson(oldLessionMain.ParagraphJson);
                    oldParagraphs.RemoveAll(p => p.LessionInfoID == model.ID);
                    oldLessionMain.ParagraphJson = _lessionService.SerializeParagraphJson(oldParagraphs);
                    oldLessionMain.ModifyUser = CreateUser;
                    oldLessionMain.ModifyDept = CreateDept;
                    await _lessionService.UpdateLessionMainAsync(oldLessionMain);
                }

                // 加入到新課程
                var newLessionMain = await _lessionService.GetLessionMainByIdAsync(model.LessionMainID.Value);
                if (newLessionMain != null)
                {
                    var newParagraphs = _lessionService.ParseParagraphJson(newLessionMain.ParagraphJson);
                    // 檢查是否已經存在
                    if (!newParagraphs.Any(p => p.LessionInfoID == model.ID))
                    {
                        newParagraphs.Add(new ParagraphInfo
                        {
                            LessionInfoID = model.ID,
                            Paragraph = model.Title
                        });
                        newLessionMain.ParagraphJson = _lessionService.SerializeParagraphJson(newParagraphs);
                        newLessionMain.ModifyUser = CreateUser;
                        newLessionMain.ModifyDept = CreateDept;
                        await _lessionService.UpdateLessionMainAsync(newLessionMain);
                    }
                }
            }
            else if (model.LessionMainID.HasValue)
            {
                // 課程沒變，但需要更新 ParagraphJson 中的標題（如果標題有變更）
                var lessionMain = await _lessionService.GetLessionMainByIdAsync(model.LessionMainID.Value);
                if (lessionMain != null)
                {
                    var paragraphs = _lessionService.ParseParagraphJson(lessionMain.ParagraphJson);
                    var paragraph = paragraphs.FirstOrDefault(p => p.LessionInfoID == model.ID);
                    if (paragraph != null && paragraph.Paragraph != model.Title)
                    {
                        paragraph.Paragraph = model.Title;
                        lessionMain.ParagraphJson = _lessionService.SerializeParagraphJson(paragraphs);
                        lessionMain.ModifyUser = CreateUser;
                        lessionMain.ModifyDept = CreateDept;
                        await _lessionService.UpdateLessionMainAsync(lessionMain);
                    }
                }
            }

            TempData["SuccessMessage"] = "課程片段修改成功！";
            return RedirectToAction(nameof(Index));
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
                var httpClient = _httpClientFactory.CreateClient();
                var apiUrl = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={apiKey}&part=contentDetails";
                var httpResponse = await httpClient.GetAsync(apiUrl);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "YouTube API 呼叫失敗" });
                }

                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<YouTubeApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                // 檢查是否有找到影片
                if (response?.Items == null || response.Items.Count == 0)
                {
                    return Json(new { success = false, message = "找不到該影片，請確認 URL 是否正確" });
                }

                // 取得影片長度（ISO 8601 格式，例如：PT1H30M45S）
                var duration = response.Items[0].ContentDetails?.Duration;

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
        /// 解析時間字串為秒數（int）
        /// </summary>
        private bool TryParseTimeToSeconds(string timeString, out int result)
        {
            result = 0;
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
                    result = hours * 3600 + minutes * 60 + seconds;
                    return true;
                }
            }
            else if (parts.Length == 2)
            {
                // MM:SS
                if (int.TryParse(parts[0], out int minutes) &&
                    int.TryParse(parts[1], out int seconds))
                {
                    result = minutes * 60 + seconds;
                    return true;
                }
            }
            else if (parts.Length == 1)
            {
                // 純秒數
                if (int.TryParse(parts[0], out int totalSeconds))
                {
                    result = totalSeconds;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 從 Session 獲取 EmployeeMainID
        /// </summary>
        private Guid GetEmployeeMainIDFromSession()
        {
            try
            {
                if (HttpContext?.Session != null)
                {
                    var employeeMainIDStr = HttpContext.Session.GetString("EmployeeMainID");
                    if (!string.IsNullOrEmpty(employeeMainIDStr) && Guid.TryParse(employeeMainIDStr, out Guid employeeMainID))
                    {
                        return employeeMainID;
                    }
                }
            }
            catch
            {
                // Session 可能未啟用或無法訪問
            }

            // 如果 Session 中沒有，嘗試從 User.Identity.Name 獲取（向後相容）
            try
            {
                var identityName = _httpAccessor.HttpContext?.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(identityName) && Guid.TryParse(identityName, out Guid employeeMainID))
                {
                    return employeeMainID;
                }
            }
            catch
            {
                // 無法從 Identity 獲取
            }

            return Guid.Empty;
        }

        /// <summary>
        /// 從 Session 獲取 CreateUser
        /// </summary>
        private Guid GetCreateUserFromSession()
        {
            try
            {
                if (HttpContext?.Session != null)
                {
                    var createUserStr = HttpContext.Session.GetString("CreateUser");
                    if (!string.IsNullOrEmpty(createUserStr) && Guid.TryParse(createUserStr, out Guid createUser))
                    {
                        return createUser;
                    }
                }
            }
            catch
            {
                // Session 可能未啟用或無法訪問
            }

            // 如果 Session 中沒有，嘗試從 User.Identity.Name 獲取（向後相容）
            try
            {
                var identityName = _httpAccessor.HttpContext?.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(identityName) && Guid.TryParse(identityName, out Guid createUser))
                {
                    return createUser;
                }
            }
            catch
            {
                // 無法從 Identity 獲取
            }

            return Guid.Empty;
        }

        /// <summary>
        /// 從 Session 獲取 CreateDept
        /// </summary>
        private Guid GetCreateDeptFromSession()
        {
            try
            {
                if (HttpContext?.Session != null)
                {
                    var createDeptStr = HttpContext.Session.GetString("CreateDept");
                    if (!string.IsNullOrEmpty(createDeptStr) && Guid.TryParse(createDeptStr, out Guid createDept))
                    {
                        return createDept;
                    }
                }
            }
            catch
            {
                // Session 可能未啟用或無法訪問
            }

            // Session 中沒有 CreateDept 時返回空值
            // 注意：Identity 中沒有標準的部門 ID 屬性，需要透過 Session 設定
            return Guid.Empty;
        }
    }

    /// <summary>
    /// 課程列表 ViewModel
    /// </summary>
    public class LessionIndexViewModel
    {
        public List<LessionMainProgress> LessionMainProgressList { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 課程播放 ViewModel
    /// </summary>
    public class LessionPlayViewModel
    {
        public EMTrainingInfo TrainingInfo { get; set; } = null!;
        public Guid EmployeeMainID { get; set; }
        public int WatchedTime { get; set; } // 已觀看時間（秒）
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 更新進度請求
    /// </summary>
    public class UpdateProgressRequest
    {
        public string LessionInfoID { get; set; } = string.Empty;
        public int WatchedTime { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 多片段課程播放 ViewModel
    /// </summary>
    public class LessionMultiSegmentViewModel
    {
        public Guid LessionMainID { get; set; }
        public string LessionMainName { get; set; } = string.Empty;
        public List<EMTrainingInfo> Segments { get; set; } = new();
        public Dictionary<Guid, EMTrainingDetl?> ProgressDict { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 新增課程 ViewModel
    /// </summary>
    public class LessionCreateViewModel
    {
        [Display(Name = "選擇課程")]
        public Guid? LessionMainID { get; set; }

        [Display(Name = "建立新課程")]
        public bool IsNewLessionMain { get; set; }

        [Display(Name = "新課程名稱")]
        public string? NewLessionMainName { get; set; }

        public List<SelectListItem> LessionMainList { get; set; } = new();

        [Required(ErrorMessage = "標題為必填")]
        [Display(Name = "片段標題")]
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

        [Display(Name = "圖片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string? Img { get; set; }

        [Display(Name = "影片標籤/錨點")]
        public string? TagJson { get; set; }
    }

    /// <summary>
    /// 修改課程 ViewModel
    /// </summary>
    public class LessionEditViewModel
    {
        public Guid ID { get; set; }

        [Display(Name = "選擇課程")]
        [Required(ErrorMessage = "請選擇課程")]
        public Guid? LessionMainID { get; set; }

        public List<SelectListItem> LessionMainList { get; set; } = new();

        [Required(ErrorMessage = "標題為必填")]
        [Display(Name = "片段標題")]
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

        [Display(Name = "圖片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string? Img { get; set; }

        [Display(Name = "影片標籤/錨點")]
        public string? TagJson { get; set; }
    }

    /// <summary>
    /// 取得 YouTube 影片資訊請求
    /// </summary>
    public class GetYouTubeVideoInfoRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 課程管理列表 ViewModel
    /// </summary>
    public class LessionSearchListViewModel
    {
        public List<LessionMainWithSegmentCount> LessionListWithSegmentCount { get; set; } = new();
    }

    /// <summary>
    /// 包含片段數量的課程資訊
    /// </summary>
    public class LessionMainWithSegmentCount
    {
        public LessionMain LessionMain { get; set; } = null!;
        public int SegmentCount { get; set; }
    }

    /// <summary>
    /// 課程詳細資訊 ViewModel
    /// </summary>
    public class LessionDetailsViewModel
    {
        public LessionMain LessionMain { get; set; } = null!;
        public List<EMTrainingInfo> Segments { get; set; } = new();
    }

    /// <summary>
    /// YouTube API 響應模型
    /// </summary>
    public class YouTubeApiResponse
    {
        public List<YouTubeVideoItem> Items { get; set; } = new();
    }

    /// <summary>
    /// YouTube 影片項目
    /// </summary>
    public class YouTubeVideoItem
    {
        public YouTubeContentDetails? ContentDetails { get; set; }
    }

    /// <summary>
    /// YouTube 內容詳情
    /// </summary>
    public class YouTubeContentDetails
    {
        public string? Duration { get; set; }
    }
}

