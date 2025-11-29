using LifeTech.ERP.Emanage.Web.Models.Models.Lession;
using LifeTech.ERP.Emanage.Web.Models.Respository.Lession;
using System.Text.Json;

namespace LifeTech.ERP.Emanage.Web.Service.Service.Lession
{
    /// <summary>
    /// 課程紀錄服務
    /// 處理課程相關的業務邏輯
    /// </summary>
    public class LessionService
    {
        private readonly LessionRepository _lessionRepository;

        public LessionService(LessionRepository lessionRepository)
        {
            _lessionRepository = lessionRepository;
        }

        /// <summary>
        /// 取得所有訓練資訊清單
        /// </summary>
        public async Task<List<EMTrainingInfo>> GetTrainingInfoListAsync()
        {
            return await _lessionRepository.GetTrainingInfoListAsync();
        }

        /// <summary>
        /// 根據 ID 取得訓練資訊
        /// </summary>
        public async Task<EMTrainingInfo?> GetTrainingInfoByIdAsync(Guid id)
        {
            return await _lessionRepository.GetTrainingInfoByIdAsync(id);
        }

        /// <summary>
        /// 取得員工的訓練明細記錄
        /// </summary>
        public async Task<EMTrainingDetl?> GetTrainingDetlAsync(Guid employeeMainID, Guid trainingInfoMainID)
        {
            return await _lessionRepository.GetTrainingDetlAsync(employeeMainID, trainingInfoMainID);
        }

        /// <summary>
        /// 取得員工所有訓練明細記錄
        /// </summary>
        public async Task<List<EMTrainingDetl>> GetTrainingDetlListAsync(Guid employeeMainID)
        {
            return await _lessionRepository.GetTrainingDetlListAsync(employeeMainID);
        }

        /// <summary>
        /// 更新影片觀看進度（含驗證）
        /// </summary>
        public async Task<(bool IsSuccess, string Message)> UpdateVideoProgressAsync(
            string lessionInfoID,
            int watchedTime,
            bool isCompleted,
            Guid employeeMainID,
            Guid currentUserID,
            Guid currentDeptID)
        {
            // 驗證請求參數
            if (string.IsNullOrEmpty(lessionInfoID))
            {
                return (false, "請求參數為空");
            }

            // 解析 GUID
            if (!Guid.TryParse(lessionInfoID, out Guid trainingInfoMainID) || 
                trainingInfoMainID == Guid.Empty)
            {
                return (false, "無效的訓練資訊ID");
            }

            // 驗證觀看時間（必須 >= 0）
            if (watchedTime < 0)
            {
                watchedTime = 0;
            }

            // 驗證用戶資訊
            if (employeeMainID == Guid.Empty || currentUserID == Guid.Empty || currentDeptID == Guid.Empty)
            {
                return (false, "無法取得用戶資訊，請重新登入");
            }

            var detl = new EMTrainingDetl
            {
                EmployeeMainID = employeeMainID,
                LessionInfoID = trainingInfoMainID,
                Time = watchedTime,
                State = isCompleted,
                ModifyUser = currentUserID,
                ModifyDept = currentDeptID
            };

            var result = await _lessionRepository.UpsertTrainingDetlAsync(detl);
            return result ? (true, "進度已更新") : (false, "更新進度失敗");
        }

        /// <summary>
        /// 更新影片觀看進度（直接使用GUID，不含驗證）
        /// </summary>
        public async Task<bool> UpdateVideoProgressDirectAsync(
            Guid employeeMainID,
            Guid trainingInfoMainID,
            int watchedTime,
            bool isCompleted,
            Guid currentUserID,
            Guid currentDeptID)
        {
            var detl = new EMTrainingDetl
            {
                EmployeeMainID = employeeMainID,
                LessionInfoID = trainingInfoMainID,
                Time = watchedTime,
                State = isCompleted,
                ModifyUser = currentUserID,
                ModifyDept = currentDeptID
            };

            return await _lessionRepository.UpsertTrainingDetlAsync(detl);
        }

        /// <summary>
        /// 取得員工的訓練進度統計（針對 LessionInfo）
        /// </summary>
        public async Task<Dictionary<Guid, TrainingProgress>> GetTrainingProgressAsync(Guid employeeMainID)
        {
            var trainingInfoList = await GetTrainingInfoListAsync();
            var trainingDetlList = await GetTrainingDetlListAsync(employeeMainID);

            var progressDict = new Dictionary<Guid, TrainingProgress>();

            foreach (var info in trainingInfoList)
            {
                var detl = trainingDetlList.FirstOrDefault(d => d.LessionInfoID == info.ID);
                
                progressDict[info.ID] = new TrainingProgress
                {
                    TrainingInfo = info,
                    WatchedTime = detl?.Time ?? 0, // 直接使用秒數（int）
                    IsCompleted = detl?.State ?? false,
                    TotalTime = info.URLTime, // 直接使用秒數（int）
                    ProgressPercentage = info.URLTime > 0 
                        ? (int)((detl?.Time ?? 0) * 100.0 / info.URLTime)
                        : 0
                };
            }

            return progressDict;
        }

        /// <summary>
        /// 取得 LessionMain 的整體進度統計
        /// </summary>
        public async Task<Dictionary<Guid, LessionMainProgress>> GetLessionMainProgressAsync(Guid employeeMainID)
        {
            var lessionMainList = await GetLessionMainListAsync();
            var trainingDetlList = await GetTrainingDetlListAsync(employeeMainID);
            var allTrainingInfoList = await GetTrainingInfoListAsync();

            var progressDict = new Dictionary<Guid, LessionMainProgress>();

            foreach (var lessionMain in lessionMainList)
            {
                // 解析 ParagraphJson 取得相關的 LessionInfo ID
                var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
                var segmentIds = paragraphs.Select(p => p.LessionInfoID).ToList();

                if (!segmentIds.Any())
                {
                    // 如果沒有片段，進度為 0
                    progressDict[lessionMain.ID] = new LessionMainProgress
                    {
                        LessionMain = lessionMain,
                        TotalSegments = 0,
                        CompletedSegments = 0,
                        TotalTime = 0,
                        WatchedTime = 0,
                        ProgressPercentage = 0,
                        IsCompleted = false
                    };
                    continue;
                }

                // 取得所有相關的 LessionInfo
                var segments = allTrainingInfoList.Where(t => segmentIds.Contains(t.ID)).ToList();
                
                // 計算總時長和已觀看時間
                int totalTime = 0;
                int watchedTime = 0;
                int completedSegments = 0;

                foreach (var segment in segments)
                {
                    totalTime += segment.URLTime;
                    var detl = trainingDetlList.FirstOrDefault(d => d.LessionInfoID == segment.ID);
                    if (detl != null)
                    {
                        watchedTime += detl.Time;
                        if (detl.State)
                        {
                            completedSegments++;
                        }
                    }
                }

                // 計算進度百分比
                int progressPercentage = totalTime > 0 ? (int)(watchedTime * 100.0 / totalTime) : 0;
                bool isCompleted = completedSegments == segments.Count && segments.Count > 0;

                progressDict[lessionMain.ID] = new LessionMainProgress
                {
                    LessionMain = lessionMain,
                    TotalSegments = segments.Count,
                    CompletedSegments = completedSegments,
                    TotalTime = totalTime,
                    WatchedTime = watchedTime,
                    ProgressPercentage = progressPercentage,
                    IsCompleted = isCompleted
                };
            }

            return progressDict;
        }

        /// <summary>
        /// 新增訓練資訊
        /// </summary>
        public async Task<Guid> CreateTrainingInfoAsync(EMTrainingInfo trainingInfo)
        {
            return await _lessionRepository.InsertTrainingInfoAsync(trainingInfo);
        }

        /// <summary>
        /// 更新訓練資訊
        /// </summary>
        public async Task<bool> UpdateTrainingInfoAsync(EMTrainingInfo trainingInfo)
        {
            return await _lessionRepository.UpdateTrainingInfoAsync(trainingInfo);
        }

        /// <summary>
        /// 取得最大 Page 編號
        /// </summary>
        public async Task<int> GetMaxPageAsync()
        {
            return await _lessionRepository.GetMaxPageAsync();
        }

        #region LessionMain 相關方法

        /// <summary>
        /// 取得所有啟用的主要課程清單
        /// </summary>
        public async Task<List<LessionMain>> GetLessionMainListAsync()
        {
            return await _lessionRepository.GetLessionMainListAsync();
        }

        /// <summary>
        /// 根據 ID 取得主要課程
        /// </summary>
        public async Task<LessionMain?> GetLessionMainByIdAsync(Guid id)
        {
            return await _lessionRepository.GetLessionMainByIdAsync(id);
        }

        /// <summary>
        /// 新增主要課程
        /// </summary>
        public async Task<Guid> CreateLessionMainAsync(LessionMain lessionMain)
        {
            return await _lessionRepository.InsertLessionMainAsync(lessionMain);
        }

        /// <summary>
        /// 更新主要課程
        /// </summary>
        public async Task<bool> UpdateLessionMainAsync(LessionMain lessionMain)
        {
            return await _lessionRepository.UpdateLessionMainAsync(lessionMain);
        }

        /// <summary>
        /// 刪除主要課程（軟刪除，將 Deleted 設為 1）
        /// </summary>
        public async Task<bool> DeleteLessionMainAsync(Guid id, Guid modifyUser, Guid modifyDept)
        {
            return await _lessionRepository.DeleteLessionMainAsync(id, modifyUser, modifyDept);
        }

        /// <summary>
        /// 解析 ParagraphJson 為段落資訊清單
        /// </summary>
        public List<ParagraphInfo> ParseParagraphJson(string? paragraphJson)
        {
            if (string.IsNullOrWhiteSpace(paragraphJson))
            {
                return new List<ParagraphInfo>();
            }

            try
            {
                var result = JsonSerializer.Deserialize<List<ParagraphInfo>>(paragraphJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result ?? new List<ParagraphInfo>();
            }
            catch
            {
                return new List<ParagraphInfo>();
            }
        }

        /// <summary>
        /// 將段落資訊清單序列化為 JSON
        /// </summary>
        public string SerializeParagraphJson(List<ParagraphInfo> paragraphs)
        {
            if (paragraphs == null || paragraphs.Count == 0)
            {
                return string.Empty;
            }

            try
            {
                return JsonSerializer.Serialize(paragraphs, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 根據主要課程 ID 取得相關的訓練資訊清單（按照 ParagraphJson 中的順序）
        /// </summary>
        public async Task<List<EMTrainingInfo>> GetTrainingInfoByLessionMainIdAsync(Guid lessionMainId)
        {
            var lessionMain = await GetLessionMainByIdAsync(lessionMainId);
            if (lessionMain == null || string.IsNullOrWhiteSpace(lessionMain.ParagraphJson))
            {
                return new List<EMTrainingInfo>();
            }

            var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
            var allTrainingInfo = await GetTrainingInfoListAsync();
            
            // 建立 ID 到 TrainingInfo 的對應字典，提高查詢效率
            var trainingInfoDict = allTrainingInfo.ToDictionary(t => t.ID, t => t);
            
            // 按照 ParagraphJson 中的順序返回片段
            var result = new List<EMTrainingInfo>();
            foreach (var paragraph in paragraphs)
            {
                if (trainingInfoDict.TryGetValue(paragraph.LessionInfoID, out var trainingInfo))
                {
                    result.Add(trainingInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// 取得多片段課程播放所需的資料（含進度）
        /// </summary>
        public async Task<(LessionMain? LessionMain, List<EMTrainingInfo> Segments, Dictionary<Guid, EMTrainingDetl?> ProgressDict, string ErrorMessage)> 
            GetMultiSegmentPlayDataAsync(Guid lessionMainID, Guid employeeMainID)
        {
            if (employeeMainID == Guid.Empty)
            {
                return (null, new List<EMTrainingInfo>(), new Dictionary<Guid, EMTrainingDetl?>(), "無法取得用戶資訊，請重新登入");
            }

            var lessionMain = await GetLessionMainByIdAsync(lessionMainID);
            if (lessionMain == null)
            {
                return (null, new List<EMTrainingInfo>(), new Dictionary<Guid, EMTrainingDetl?>(), "找不到課程");
            }

            var segments = await GetTrainingInfoByLessionMainIdAsync(lessionMainID);
            if (!segments.Any())
            {
                return (null, new List<EMTrainingInfo>(), new Dictionary<Guid, EMTrainingDetl?>(), "找不到課程片段");
            }

            // 取得所有片段的觀看進度
            var progressDict = new Dictionary<Guid, EMTrainingDetl?>();
            foreach (var segment in segments)
            {
                try
                {
                    var detl = await GetTrainingDetlAsync(employeeMainID, segment.ID);
                    progressDict[segment.ID] = detl;
                }
                catch
                {
                    progressDict[segment.ID] = null;
                }
            }

            return (lessionMain, segments, progressDict, string.Empty);
        }

        /// <summary>
        /// 根據 VideoUrl 找到對應的 TrainingInfo
        /// </summary>
        public async Task<EMTrainingInfo?> FindTrainingInfoByVideoUrlAsync(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl))
            {
                return null;
            }

            var trainingInfoList = await GetTrainingInfoListAsync();
            var videoId = ExtractVideoId(videoUrl);

            return trainingInfoList.FirstOrDefault(t => 
                t.URL == videoUrl || 
                t.URL.Contains(videoUrl) || 
                videoUrl.Contains(t.URL) ||
                ExtractVideoId(t.URL) == videoId);
        }

        /// <summary>
        /// 儲存觀看時間（相容舊版 API）
        /// </summary>
        public async Task<(bool Success, string Message)> SaveTimeAsync(
            string time, 
            string urlTime, 
            string videoUrl,
            Guid employeeMainID,
            Guid createUser,
            Guid createDept)
        {
            if (employeeMainID == Guid.Empty || createUser == Guid.Empty || createDept == Guid.Empty)
            {
                return (false, "無法取得用戶資訊");
            }

            var trainingInfo = await FindTrainingInfoByVideoUrlAsync(videoUrl);
            if (trainingInfo == null)
            {
                return (false, "找不到對應的訓練資訊");
            }

            // 解析時間字串 (格式: HH:MM:SS)
            if (!TryParseTimeString(urlTime, out int watchedTimeSeconds))
            {
                return (false, "時間格式錯誤");
            }

            // 判斷是否完成（如果觀看時間接近總時長）
            var isCompleted = watchedTimeSeconds >= (trainingInfo.URLTime * 0.95);

            var result = await UpdateVideoProgressDirectAsync(
                employeeMainID,
                trainingInfo.ID,
                watchedTimeSeconds,
                isCompleted,
                createUser,
                createDept
            );

            return (result, result ? "成功" : "失敗");
        }

        /// <summary>
        /// 根據 TrainingInfoID 找出所屬的 LessionMain
        /// </summary>
        public async Task<Guid?> FindLessionMainIdByTrainingInfoIdAsync(Guid trainingInfoID)
        {
            var lessionMainList = await GetLessionMainListAsync();

            foreach (var lessionMain in lessionMainList)
            {
                var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
                if (paragraphs.Any(p => p.LessionInfoID == trainingInfoID))
                {
                    return lessionMain.ID;
                }
            }

            return null;
        }

        /// <summary>
        /// 取得課程列表及其片段數量
        /// </summary>
        public async Task<List<LessionMainWithSegmentCount>> GetLessionListWithSegmentCountAsync()
        {
            var lessionMainList = await GetLessionMainListAsync();
            
            return lessionMainList.Select(lessionMain =>
            {
                var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
                return new LessionMainWithSegmentCount
                {
                    LessionMain = lessionMain,
                    SegmentCount = paragraphs?.Count ?? 0
                };
            }).ToList();
        }

        /// <summary>
        /// 新增課程片段（含完整業務邏輯）
        /// </summary>
        public async Task<(bool IsSuccess, string Message, Guid? TrainingInfoID, Guid? LessionMainID)> CreateLessionSegmentAsync(
            LessionCreateRequest request,
            Guid createUser,
            Guid createDept)
        {
            if (createUser == Guid.Empty || createDept == Guid.Empty)
            {
                return (false, "無法取得用戶資訊", null, null);
            }

            // 解析時間字串
            if (!TryParseTimeToSeconds(request.URLTimeString, out int urlTimeSeconds))
            {
                return (false, "時間格式錯誤，請使用 HH:MM:SS 或 MM:SS 格式", null, null);
            }

            Guid lessionMainID;

            // 判斷是新增新課程還是選擇現有課程
            if (request.IsNewLessionMain)
            {
                if (string.IsNullOrWhiteSpace(request.NewLessionMainName))
                {
                    return (false, "新課程名稱為必填", null, null);
                }

                var newLessionMain = new LessionMain
                {
                    LessionName = request.NewLessionMainName,
                    ParagraphJson = string.Empty,
                    ModifyUser = createUser,
                    ModifyDept = createDept
                };

                var createLessionMainID = await CreateLessionMainAsync(newLessionMain);
                if (createLessionMainID == Guid.Empty)
                {
                    return (false, "建立新課程失敗，請稍後再試", null, null);
                }

                lessionMainID = createLessionMainID;
            }
            else
            {
                if (!request.LessionMainID.HasValue)
                {
                    return (false, "請選擇課程或建立新課程", null, null);
                }

                lessionMainID = request.LessionMainID.Value;
            }

            // 新增 LessionInfo（片段）
            var trainingInfo = new EMTrainingInfo
            {
                Title = request.Title,
                Content = request.Content ?? string.Empty,
                URL = request.URL,
                URLTime = urlTimeSeconds,
                Img = request.Img,
                Page = 0,
                TagJson = request.TagJson,
                ModifyUser = createUser,
                ModifyDept = createDept
            };

            var createTrainingInfoGuid = await CreateTrainingInfoAsync(trainingInfo);
            if (createTrainingInfoGuid == Guid.Empty)
            {
                return (false, "新增片段失敗，請稍後再試", null, lessionMainID);
            }

            // 更新 LessionMain 的 ParagraphJson
            var lessionMain = await GetLessionMainByIdAsync(lessionMainID);
            if (lessionMain == null)
            {
                return (false, "找不到對應的課程，請重新選擇", createTrainingInfoGuid, lessionMainID);
            }

            var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
            paragraphs.Add(new ParagraphInfo
            {
                LessionInfoID = createTrainingInfoGuid,
                Paragraph = request.Title
            });

            lessionMain.ParagraphJson = SerializeParagraphJson(paragraphs);
            lessionMain.ModifyUser = createUser;
            lessionMain.ModifyDept = createDept;

            var updateResult = await UpdateLessionMainAsync(lessionMain);
            if (!updateResult)
            {
                return (false, "更新課程資訊失敗，但片段已新增", createTrainingInfoGuid, lessionMainID);
            }

            return (true, "課程片段新增成功", createTrainingInfoGuid, lessionMainID);
        }

        /// <summary>
        /// 更新課程片段（含完整業務邏輯）
        /// </summary>
        public async Task<(bool IsSuccess, string Message)> UpdateLessionSegmentAsync(
            LessionEditRequest request,
            Guid createUser,
            Guid createDept)
        {
            if (createUser == Guid.Empty || createDept == Guid.Empty)
            {
                return (false, "無法取得用戶資訊");
            }

            var trainingInfo = await GetTrainingInfoByIdAsync(request.ID);
            if (trainingInfo == null)
            {
                return (false, "找不到要修改的訓練資訊");
            }

            // 解析時間字串
            if (!TryParseTimeToSeconds(request.URLTimeString, out int urlTimeSeconds))
            {
                return (false, "時間格式錯誤，請使用 HH:MM:SS 或 MM:SS 格式");
            }

            if (!request.LessionMainID.HasValue)
            {
                return (false, "請選擇課程");
            }

            // 找出該片段原本所屬的課程
            var oldLessionMainID = await FindLessionMainIdByTrainingInfoIdAsync(request.ID);

            // 更新訓練資訊
            trainingInfo.Title = request.Title;
            trainingInfo.Content = request.Content ?? string.Empty;
            trainingInfo.URL = request.URL;
            trainingInfo.URLTime = urlTimeSeconds;
            trainingInfo.Img = request.Img;
            trainingInfo.TagJson = request.TagJson;
            trainingInfo.ModifyUser = createUser;
            trainingInfo.ModifyDept = createDept;

            var updateResult = await UpdateTrainingInfoAsync(trainingInfo);
            if (!updateResult)
            {
                return (false, "更新片段失敗，請稍後再試");
            }

            // 如果課程有變更，需要更新兩個課程的 ParagraphJson
            if (oldLessionMainID.HasValue && oldLessionMainID.Value != request.LessionMainID.Value)
            {
                // 從舊課程移除
                var oldLessionMain = await GetLessionMainByIdAsync(oldLessionMainID.Value);
                if (oldLessionMain != null)
                {
                    var oldParagraphs = ParseParagraphJson(oldLessionMain.ParagraphJson);
                    oldParagraphs.RemoveAll(p => p.LessionInfoID == request.ID);
                    oldLessionMain.ParagraphJson = SerializeParagraphJson(oldParagraphs);
                    oldLessionMain.ModifyUser = createUser;
                    oldLessionMain.ModifyDept = createDept;
                    await UpdateLessionMainAsync(oldLessionMain);
                }

                // 加入到新課程
                var newLessionMain = await GetLessionMainByIdAsync(request.LessionMainID.Value);
                if (newLessionMain != null)
                {
                    var newParagraphs = ParseParagraphJson(newLessionMain.ParagraphJson);
                    if (!newParagraphs.Any(p => p.LessionInfoID == request.ID))
                    {
                        newParagraphs.Add(new ParagraphInfo
                        {
                            LessionInfoID = request.ID,
                            Paragraph = request.Title
                        });
                        newLessionMain.ParagraphJson = SerializeParagraphJson(newParagraphs);
                        newLessionMain.ModifyUser = createUser;
                        newLessionMain.ModifyDept = createDept;
                        await UpdateLessionMainAsync(newLessionMain);
                    }
                }
            }
            else if (request.LessionMainID.HasValue)
            {
                // 課程沒變，但需要更新 ParagraphJson 中的標題（如果標題有變更）
                var lessionMain = await GetLessionMainByIdAsync(request.LessionMainID.Value);
                if (lessionMain != null)
                {
                    var paragraphs = ParseParagraphJson(lessionMain.ParagraphJson);
                    var paragraph = paragraphs.FirstOrDefault(p => p.LessionInfoID == request.ID);
                    if (paragraph != null && paragraph.Paragraph != request.Title)
                    {
                        paragraph.Paragraph = request.Title;
                        lessionMain.ParagraphJson = SerializeParagraphJson(paragraphs);
                        lessionMain.ModifyUser = createUser;
                        lessionMain.ModifyDept = createDept;
                        await UpdateLessionMainAsync(lessionMain);
                    }
                }
            }

            return (true, "課程片段修改成功");
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 從 YouTube URL 中提取 Video ID
        /// </summary>
        public string ExtractVideoId(string url)
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
        /// 解析時間字串為秒數（支援 HH:MM:SS, MM:SS, 或純秒數）
        /// </summary>
        public bool TryParseTimeToSeconds(string timeString, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(timeString))
                return false;

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
        /// 解析時間字串（格式: HH:MM:SS）為秒數
        /// </summary>
        private bool TryParseTimeString(string urlTime, out int result)
        {
            result = 0;
            var timeParts = urlTime.Split(':');
            if (timeParts.Length != 3)
            {
                return false;
            }

            if (int.TryParse(timeParts[0], out int hours) &&
                int.TryParse(timeParts[1], out int minutes) &&
                int.TryParse(timeParts[2], out int seconds))
            {
                result = hours * 3600 + minutes * 60 + seconds;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 將秒數轉換為時間字串格式（HH:MM:SS 或 MM:SS）
        /// </summary>
        public string FormatTimeSpan(int totalSeconds)
        {
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            else
            {
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 解析 ISO 8601 時間格式（例如：PT1H30M45S）為秒數
        /// </summary>
        public int ParseISO8601Duration(string duration)
        {
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

        #endregion
    }

    /// <summary>
    /// 訓練進度資訊（針對單一 LessionInfo）
    /// </summary>
    public class TrainingProgress
    {
        public EMTrainingInfo TrainingInfo { get; set; } = null!;
        public int WatchedTime { get; set; } // 已觀看時間（秒）
        public bool IsCompleted { get; set; }
        public int TotalTime { get; set; } // 總時長（秒）
        public int ProgressPercentage { get; set; }
    }

    /// <summary>
    /// LessionMain 整體進度資訊
    /// </summary>
    public class LessionMainProgress
    {
        public LessionMain LessionMain { get; set; } = null!;
        public int TotalSegments { get; set; } // 總片段數
        public int CompletedSegments { get; set; } // 已完成片段數
        public int TotalTime { get; set; } // 總時長（秒）
        public int WatchedTime { get; set; } // 已觀看時間（秒）
        public int ProgressPercentage { get; set; } // 進度百分比
        public bool IsCompleted { get; set; } // 是否全部完成
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
    /// 新增課程片段請求
    /// </summary>
    public class LessionCreateRequest
    {
        public Guid? LessionMainID { get; set; }
        public bool IsNewLessionMain { get; set; }
        public string? NewLessionMainName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string URL { get; set; } = string.Empty;
        public string URLTimeString { get; set; } = string.Empty;
        public string? Img { get; set; }
        public string? TagJson { get; set; }
    }

    /// <summary>
    /// 更新課程片段請求
    /// </summary>
    public class LessionEditRequest
    {
        public Guid ID { get; set; }
        public Guid? LessionMainID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string URL { get; set; } = string.Empty;
        public string URLTimeString { get; set; } = string.Empty;
        public string? Img { get; set; }
        public string? TagJson { get; set; }
    }
}

