using ERP.Web.Models.Models.Lession;
using ERP.Web.Models.Respository.Lession;
using System.Text.Json;

namespace ERP.Web.Service.Service.Lession
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
        /// 更新影片觀看進度
        /// </summary>
        /// <param name="employeeMainID">員工ID</param>
        /// <param name="trainingInfoMainID">訓練資訊ID</param>
        /// <param name="watchedTime">已觀看時間（秒）</param>
        /// <param name="isCompleted">是否看完</param>
        /// <param name="currentUserID">當前使用者ID</param>
        /// <param name="currentDeptID">當前部門ID</param>
        public async Task<bool> UpdateVideoProgressAsync(
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
                Time = watchedTime, // 直接使用秒數（int）
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
}

