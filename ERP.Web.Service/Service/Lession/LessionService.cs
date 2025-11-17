using ERP.Web.Models.Models;
using ERP.Web.Models.Respository.Lession;

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
                EMTrainingInfoMainID = trainingInfoMainID,
                Time = TimeSpan.FromSeconds(watchedTime),
                State = isCompleted,
                CreateUser = currentUserID,
                CreateDept = currentDeptID,
                ModifyUser = currentUserID,
                ModifyDept = currentDeptID
            };

            return await _lessionRepository.UpsertTrainingDetlAsync(detl);
        }

        /// <summary>
        /// 取得員工的訓練進度統計
        /// </summary>
        public async Task<Dictionary<Guid, TrainingProgress>> GetTrainingProgressAsync(Guid employeeMainID)
        {
            var trainingInfoList = await GetTrainingInfoListAsync();
            var trainingDetlList = await GetTrainingDetlListAsync(employeeMainID);

            var progressDict = new Dictionary<Guid, TrainingProgress>();

            foreach (var info in trainingInfoList)
            {
                var detl = trainingDetlList.FirstOrDefault(d => d.EMTrainingInfoMainID == info.ID);
                
                progressDict[info.ID] = new TrainingProgress
                {
                    TrainingInfo = info,
                    WatchedTime = detl?.Time ?? TimeSpan.Zero,
                    IsCompleted = detl?.State ?? false,
                    TotalTime = info.URLTime,
                    ProgressPercentage = info.URLTime.TotalSeconds > 0 
                        ? (int)((detl?.Time.TotalSeconds ?? 0) / info.URLTime.TotalSeconds * 100)
                        : 0
                };
            }

            return progressDict;
        }

        /// <summary>
        /// 新增訓練資訊
        /// </summary>
        public async Task<bool> CreateTrainingInfoAsync(EMTrainingInfo trainingInfo)
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
    }

    /// <summary>
    /// 訓練進度資訊
    /// </summary>
    public class TrainingProgress
    {
        public EMTrainingInfo TrainingInfo { get; set; } = null!;
        public TimeSpan WatchedTime { get; set; }
        public bool IsCompleted { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int ProgressPercentage { get; set; }
    }
}

