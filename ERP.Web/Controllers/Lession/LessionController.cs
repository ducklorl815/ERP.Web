using LifeTech.ERP.Emanage.Web.Models.Models.Lession;
using LifeTech.ERP.Emanage.Web.Service.Service.Lession;
using LifeTech.ERP.Emanage.Web.Service.ViewModels;
using LifeTech.ERP.Utility.Extensions;
using LifeTech.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;


namespace LifeTech.ERP.Emanage.Web.Controllers.Lession
{
    /// <summary>
    /// 課程紀錄控制器
    /// 處理課程影片播放和進度記錄功能
    /// </summary>
    public class LessionController : Controller
    {
        private readonly LessionService _lessionService;
        private readonly IConfiguration _configuration;
        private readonly HttpClientHelper _httpClientHelper;
        private readonly IHttpContextAccessor _httpAccessor;

        public LessionController(
            LessionService lessionService, 
            IConfiguration configuration, 
            HttpClientHelper httpClientHelper,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _lessionService = lessionService;
            _configuration = configuration;
            _httpClientHelper = httpClientHelper;
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
                var (lessionMain, segments, progressDict, errorMessage) = await _lessionService.GetMultiSegmentPlayDataAsync(lessionMainID, employeeMainID);
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(errorMessage);
                }

                if (lessionMain == null)
                {
                    return NotFound();
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
                var employeeMainID = GetEmployeeMainIDFromSession();
                var createUser = GetCreateUserFromSession();
                var createDept = GetCreateDeptFromSession();

                var (isSuccess, message) = await _lessionService.UpdateVideoProgressAsync(
                    LessionInfoID,
                    WatchedTime,
                    IsCompleted,
                    employeeMainID,
                    createUser,
                    createDept
                );

                return Json(new { IsSuccess = isSuccess, Msg = message });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    IsSuccess = false,
                    Msg = $"更新進度時發生錯誤：{ex.Message}",
                    ExceptionType = ex.GetType().Name,
                    InnerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 儲存觀看時間（使用 FormData，相容舊版 API）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveTime(string time, string UrlTime, string VideoUrl)
        {
            var employeeMainID = GetEmployeeMainIDFromSession();
            var createUser = GetCreateUserFromSession();
            var createDept = GetCreateDeptFromSession();

            var (success, message) = await _lessionService.SaveTimeAsync(
                time,
                UrlTime,
                VideoUrl,
                employeeMainID,
                createUser,
                createDept
            );

            return Json(new { success, message });
        }

        /// <summary>
        /// 重置觀看進度（重看功能）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateDetl([FromForm] string LessionInfoID)
        {
            try
            {
                var employeeMainID = GetEmployeeMainIDFromSession();
                var createUser = GetCreateUserFromSession();
                var createDept = GetCreateDeptFromSession();

                var (isSuccess, message) = await _lessionService.UpdateVideoProgressAsync(
                    LessionInfoID,
                    0,
                    false,
                    employeeMainID,
                    createUser,
                    createDept
                );

                return Json(new { IsSuccess = isSuccess, Msg = isSuccess ? "已重置觀看進度" : message });
            }
            catch (Exception ex)
            {
                return Json(new { IsSuccess = false, Msg = $"重置失敗：{ex.Message}" });
            }
        }

        /// <summary>
        /// 新增課程頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(Guid? lessionMainID = null)
        {
            // 取得所有 LessionMain 列表供選擇
            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            var viewModel = new LessionCreateViewModel
            {
                LessionMainID = lessionMainID,
                LessionMainList = lessionMainList.Select(lm => new SelectListItem
                {
                    Value = lm.ID.ToString(),
                    Text = lm.LessionName,
                    Selected = lessionMainID.HasValue && lm.ID == lessionMainID.Value
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

            var createUser = GetCreateUserFromSession();
            var createDept = GetCreateDeptFromSession();

            // 處理表單中的 IsNewLessionMain 值
            bool isNewLessionMain = model.IsNewLessionMain;
            if (Request.Form.ContainsKey("IsNewLessionMain"))
            {
                var formValue = Request.Form["IsNewLessionMain"].ToString();
                isNewLessionMain = formValue == "true";
            }

            var request = new LessionCreateRequest
            {
                LessionMainID = model.LessionMainID,
                IsNewLessionMain = isNewLessionMain,
                NewLessionMainName = model.NewLessionMainName,
                Title = model.Title,
                Content = model.Content,
                URL = model.URL,
                URLTimeString = model.URLTimeString,
                Img = model.Img,
                TagJson = model.TagJson
            };

            var (isSuccess, message, trainingInfoID, lessionMainID) = await _lessionService.CreateLessionSegmentAsync(
                request,
                createUser,
                createDept
            );

            if (!isSuccess)
            {
                ModelState.AddModelError("", message);
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
            var lessionListWithSegmentCount = await _lessionService.GetLessionListWithSegmentCountAsync();
            
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
        /// 刪除課程（軟刪除）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // 驗證課程是否存在
                var lessionMain = await _lessionService.GetLessionMainByIdAsync(id);
                if (lessionMain == null)
                {
                    TempData["ErrorMessage"] = "找不到要刪除的課程";
                    return RedirectToAction(nameof(SearchList));
                }

                // 從 Session 獲取用戶資訊
                var CreateUser = GetCreateUserFromSession();
                var CreateDept = GetCreateDeptFromSession();

                if (CreateUser == Guid.Empty || CreateDept == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "無法取得用戶資訊";
                    return RedirectToAction(nameof(SearchList));
                }

                // 執行刪除（軟刪除）
                var result = await _lessionService.DeleteLessionMainAsync(id, CreateUser, CreateDept);
                if (result)
                {
                    TempData["SuccessMessage"] = "課程已成功刪除";
                }
                else
                {
                    TempData["ErrorMessage"] = "刪除課程失敗，請稍後再試";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除課程時發生錯誤：{ex.Message}";
            }

            return RedirectToAction(nameof(SearchList));
        }

        /// <summary>
        /// 修改課程片段頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var trainingInfo = await _lessionService.GetTrainingInfoByIdAsync(id);
            if (trainingInfo == null)
            {
                return NotFound();
            }

            var lessionMainList = await _lessionService.GetLessionMainListAsync();
            var currentLessionMainID = await _lessionService.FindLessionMainIdByTrainingInfoIdAsync(id);

            var viewModel = new LessionEditViewModel
            {
                ID = trainingInfo.ID,
                Title = trainingInfo.Title,
                Content = trainingInfo.Content ?? string.Empty,
                URL = trainingInfo.URL,
                URLTimeString = _lessionService.FormatTimeSpan(trainingInfo.URLTime),
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

            var createUser = GetCreateUserFromSession();
            var createDept = GetCreateDeptFromSession();

            var request = new LessionEditRequest
            {
                ID = model.ID,
                LessionMainID = model.LessionMainID,
                Title = model.Title,
                Content = model.Content,
                URL = model.URL,
                URLTimeString = model.URLTimeString,
                Img = model.Img,
                TagJson = model.TagJson
            };

            var (isSuccess, message) = await _lessionService.UpdateLessionSegmentAsync(
                request,
                createUser,
                createDept
            );

            if (!isSuccess)
            {
                ModelState.AddModelError("", message);
                return View(model);
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

                var videoId = _lessionService.ExtractVideoId(request.Url);
                if (string.IsNullOrEmpty(videoId))
                {
                    return Json(new { success = false, message = "無法識別 YouTube URL 格式" });
                }

                var apiKey = _configuration["YouTube:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Json(new { success = false, message = "YouTube API Key 未設定，請手動輸入影片時長" });
                }

                var httpRequest = new HttpRequestMessage();
                httpRequest.RequestUri = new Uri($"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={apiKey}&part=contentDetails");
                httpRequest.Method = HttpMethod.Get;
                var response = await _httpClientHelper.GetJsonAsync<YouTubeApiResponse>(httpRequest);
                
                if (response?.Items == null || response.Items.Count == 0)
                {
                    return Json(new { success = false, message = "找不到該影片，請確認 URL 是否正確" });
                }

                var duration = response.Items[0].ContentDetails?.Duration;
                if (string.IsNullOrEmpty(duration))
                {
                    return Json(new { success = false, message = "無法取得影片長度資訊" });
                }

                var totalSeconds = _lessionService.ParseISO8601Duration(duration);
                if (totalSeconds <= 0)
                {
                    return Json(new { success = false, message = "無法解析影片長度" });
                }

                var timeString = _lessionService.FormatTimeSpan(totalSeconds);

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

            // 如果 Session 中沒有，嘗試從 User.Identity 獲取（向後相容）
            try
            {
                var identityName = _httpAccessor.HttpContext?.User?.Identity?.GetName();
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

            // 如果 Session 中沒有，嘗試從 User.Identity 獲取（向後相容）
            try
            {
                var identityName = _httpAccessor.HttpContext?.User?.Identity?.GetName();
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

            // 如果 Session 中沒有，嘗試從 User.Identity 獲取（向後相容）
            try
            {
                var identityDeptID = _httpAccessor.HttpContext?.User?.Identity?.GetDeptID();
                if (!string.IsNullOrEmpty(identityDeptID) && Guid.TryParse(identityDeptID, out Guid createDept))
                {
                    return createDept;
                }
            }
            catch
            {
                // 無法從 Identity 獲取
            }

            return Guid.Empty;
        }
    }
}

