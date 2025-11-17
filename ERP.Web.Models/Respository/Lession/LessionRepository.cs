using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ERP.Web.Models.Respository.Lession
{
    /// <summary>
    /// 課程紀錄 Repository
    /// 處理所有課程相關的資料存取操作
    /// </summary>
    public class LessionRepository
    {
        private readonly DBList _dBList;

        public LessionRepository(IOptions<DBList> dbList)
        {
            _dBList = dbList.Value;
        }

        #region EMTrainingInfo 相關方法

        /// <summary>
        /// 取得所有啟用的訓練資訊清單（依分頁排序）
        /// </summary>
        public async Task<List<EMTrainingInfo>> GetTrainingInfoListAsync()
        {
            var sql = @"
                SELECT 
                    ID, Seq, myModal, Title, Content, URL, URLTime, 
                    URLStyle, TitleStyle, ContentStyle, Img, ImgStyle, Page,
                    CreateDate, CreateUser, CreateDept, 
                    ModifyDate, ModifyUser, ModifyDept, 
                    Enabled, Deleted
                FROM dbo.EMTrainingInfo
                WHERE Enabled = 1 AND Deleted = 0
                ORDER BY Page ASC, Seq ASC
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<EMTrainingInfo>(sql);
                return result.ToList();
            }
            catch
            {
                return new List<EMTrainingInfo>();
            }
        }

        /// <summary>
        /// 根據 ID 取得訓練資訊
        /// </summary>
        public async Task<EMTrainingInfo?> GetTrainingInfoByIdAsync(Guid id)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", id);

            var sql = @"
                SELECT 
                    ID, Seq, myModal, Title, Content, URL, URLTime, 
                    URLStyle, TitleStyle, ContentStyle, Img, ImgStyle, Page,
                    CreateDate, CreateUser, CreateDept, 
                    ModifyDate, ModifyUser, ModifyDept, 
                    Enabled, Deleted
                FROM dbo.EMTrainingInfo
                WHERE ID = @ID AND Enabled = 1 AND Deleted = 0
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<EMTrainingInfo>(sql, sqlparam);
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 新增訓練資訊
        /// </summary>
        public async Task<bool> InsertTrainingInfoAsync(EMTrainingInfo trainingInfo)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", Guid.NewGuid());
            sqlparam.Add("MyModal", trainingInfo.MyModal);
            sqlparam.Add("Title", trainingInfo.Title);
            sqlparam.Add("Content", trainingInfo.Content);
            sqlparam.Add("URL", trainingInfo.URL);
            sqlparam.Add("URLTime", trainingInfo.URLTime);
            sqlparam.Add("URLStyle", trainingInfo.URLStyle ?? string.Empty);
            sqlparam.Add("TitleStyle", trainingInfo.TitleStyle ?? string.Empty);
            sqlparam.Add("ContentStyle", trainingInfo.ContentStyle ?? string.Empty);
            sqlparam.Add("Img", trainingInfo.Img ?? (object)DBNull.Value);
            sqlparam.Add("ImgStyle", trainingInfo.ImgStyle ?? string.Empty);
            sqlparam.Add("Page", trainingInfo.Page);
            sqlparam.Add("CreateDate", DateTime.Now);
            sqlparam.Add("CreateUser", trainingInfo.CreateUser);
            sqlparam.Add("CreateDept", trainingInfo.CreateDept);
            sqlparam.Add("ModifyDate", DateTime.Now);
            sqlparam.Add("ModifyUser", trainingInfo.ModifyUser);
            sqlparam.Add("ModifyDept", trainingInfo.ModifyDept);
            sqlparam.Add("Enabled", true);
            sqlparam.Add("Deleted", false);

            var sql = @"
                INSERT INTO dbo.EMTrainingInfo 
                (ID, myModal, Title, Content, URL, URLTime, 
                 URLStyle, TitleStyle, ContentStyle, Img, ImgStyle, Page,
                 CreateDate, CreateUser, CreateDept, 
                 ModifyDate, ModifyUser, ModifyDept, 
                 Enabled, Deleted)
                VALUES 
                (@ID, @MyModal, @Title, @Content, @URL, @URLTime, 
                 @URLStyle, @TitleStyle, @ContentStyle, @Img, @ImgStyle, @Page,
                 @CreateDate, @CreateUser, @CreateDept, 
                 @ModifyDate, @ModifyUser, @ModifyDept, 
                 @Enabled, @Deleted)
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 更新訓練資訊
        /// </summary>
        public async Task<bool> UpdateTrainingInfoAsync(EMTrainingInfo trainingInfo)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", trainingInfo.ID);
            sqlparam.Add("MyModal", trainingInfo.MyModal);
            sqlparam.Add("Title", trainingInfo.Title);
            sqlparam.Add("Content", trainingInfo.Content);
            sqlparam.Add("URL", trainingInfo.URL);
            sqlparam.Add("URLTime", trainingInfo.URLTime);
            sqlparam.Add("URLStyle", trainingInfo.URLStyle ?? string.Empty);
            sqlparam.Add("TitleStyle", trainingInfo.TitleStyle ?? string.Empty);
            sqlparam.Add("ContentStyle", trainingInfo.ContentStyle ?? string.Empty);
            sqlparam.Add("Img", trainingInfo.Img ?? (object)DBNull.Value);
            sqlparam.Add("ImgStyle", trainingInfo.ImgStyle ?? string.Empty);
            sqlparam.Add("Page", trainingInfo.Page);
            sqlparam.Add("ModifyDate", DateTime.Now);
            sqlparam.Add("ModifyUser", trainingInfo.ModifyUser);
            sqlparam.Add("ModifyDept", trainingInfo.ModifyDept);

            var sql = @"
                UPDATE dbo.EMTrainingInfo
                SET myModal = @MyModal,
                    Title = @Title,
                    Content = @Content,
                    URL = @URL,
                    URLTime = @URLTime,
                    URLStyle = @URLStyle,
                    TitleStyle = @TitleStyle,
                    ContentStyle = @ContentStyle,
                    Img = @Img,
                    ImgStyle = @ImgStyle,
                    Page = @Page,
                    ModifyDate = @ModifyDate,
                    ModifyUser = @ModifyUser,
                    ModifyDept = @ModifyDept
                WHERE ID = @ID
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 取得最大 Page 編號
        /// </summary>
        public async Task<int> GetMaxPageAsync()
        {
            var sql = @"
                SELECT ISNULL(MAX(Page), 0) 
                FROM dbo.EMTrainingInfo
                WHERE Enabled = 1 AND Deleted = 0
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql);
                return result;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region EMTrainingDetl 相關方法

        /// <summary>
        /// 取得員工的訓練明細記錄
        /// </summary>
        public async Task<EMTrainingDetl?> GetTrainingDetlAsync(Guid employeeMainID, Guid trainingInfoMainID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("EmployeeMainID", employeeMainID);
            sqlparam.Add("EMTrainingInfoMainID", trainingInfoMainID);

            var sql = @"
                SELECT 
                    ID, Seq, EmployeeMainID, EMTrainingInfoMainID, Time, State,
                    CreateDate, CreateUser, CreateDept, 
                    ModifyDate, ModifyUser, ModifyDept, 
                    Enabled, Deleted
                FROM dbo.EMTrainingDetl
                WHERE EmployeeMainID = @EmployeeMainID 
                    AND EMTrainingInfoMainID = @EMTrainingInfoMainID
                    AND Enabled = 1 AND Deleted = 0
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<EMTrainingDetl>(sql, sqlparam);
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 取得員工所有訓練明細記錄
        /// </summary>
        public async Task<List<EMTrainingDetl>> GetTrainingDetlListAsync(Guid employeeMainID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("EmployeeMainID", employeeMainID);

            var sql = @"
                SELECT 
                    ID, Seq, EmployeeMainID, EMTrainingInfoMainID, Time, State,
                    CreateDate, CreateUser, CreateDept, 
                    ModifyDate, ModifyUser, ModifyDept, 
                    Enabled, Deleted
                FROM dbo.EMTrainingDetl
                WHERE EmployeeMainID = @EmployeeMainID
                    AND Enabled = 1 AND Deleted = 0
                ORDER BY CreateDate DESC
            ";


            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<EMTrainingDetl>(sql, sqlparam);
                return result.ToList();
            }
            catch
            {
                return new List<EMTrainingDetl>();
            }
        }

        /// <summary>
        /// 新增或更新訓練明細記錄
        /// </summary>
        public async Task<bool> UpsertTrainingDetlAsync(EMTrainingDetl detl)
        {
            // 先檢查是否已存在
            var existing = await GetTrainingDetlAsync(detl.EmployeeMainID, detl.EMTrainingInfoMainID);

            if (existing != null)
            {
                // 更新現有記錄
                var sqlparam = new DynamicParameters();
                sqlparam.Add("ID", existing.ID);
                sqlparam.Add("Time", detl.Time);
                sqlparam.Add("State", detl.State);
                sqlparam.Add("ModifyDate", DateTime.Now);
                sqlparam.Add("ModifyUser", detl.ModifyUser);
                sqlparam.Add("ModifyDept", detl.ModifyDept);

                var sql = @"
                    UPDATE dbo.EMTrainingDetl
                    SET Time = @Time,
                        State = @State,
                        ModifyDate = @ModifyDate,
                        ModifyUser = @ModifyUser,
                        ModifyDept = @ModifyDept
                    WHERE ID = @ID
                ";


                using var conn = new SqlConnection(_dBList.erp);
                try
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // 新增記錄
                var sqlparam = new DynamicParameters();
                sqlparam.Add("ID", Guid.NewGuid());
                sqlparam.Add("EmployeeMainID", detl.EmployeeMainID);
                sqlparam.Add("EMTrainingInfoMainID", detl.EMTrainingInfoMainID);
                sqlparam.Add("Time", detl.Time);
                sqlparam.Add("State", detl.State);
                sqlparam.Add("CreateDate", DateTime.Now);
                sqlparam.Add("CreateUser", detl.CreateUser);
                sqlparam.Add("CreateDept", detl.CreateDept);
                sqlparam.Add("ModifyDate", DateTime.Now);
                sqlparam.Add("ModifyUser", detl.ModifyUser);
                sqlparam.Add("ModifyDept", detl.ModifyDept);
                sqlparam.Add("Enabled", true);
                sqlparam.Add("Deleted", false);

                var sql = @"
                    INSERT INTO dbo.EMTrainingDetl 
                    (ID, EmployeeMainID, EMTrainingInfoMainID, Time, State,
                     CreateDate, CreateUser, CreateDept, 
                     ModifyDate, ModifyUser, ModifyDept, 
                     Enabled, Deleted)
                    VALUES 
                    (@ID, @EmployeeMainID, @EMTrainingInfoMainID, @Time, @State,
                     @CreateDate, @CreateUser, @CreateDept, 
                     @ModifyDate, @ModifyUser, @ModifyDept, 
                     @Enabled, @Deleted)
                ";


                using var conn = new SqlConnection(_dBList.erp);
                try
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        #endregion

        #region EMTraining 相關方法

        /// <summary>
        /// 取得員工的訓練總記錄
        /// </summary>
        public async Task<EMTraining?> GetTrainingAsync(Guid employeeMainID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("EmployeeMainID", employeeMainID);

            var sql = @"
                SELECT 
                    ID, Seq, EmployeeMainID, Time, State,
                    CreateDate, CreateUser, CreateDept, 
                    ModifyDate, ModifyUser, ModifyDept, 
                    Enabled, Deleted
                FROM dbo.EMTraining
                WHERE EmployeeMainID = @EmployeeMainID
                    AND Enabled = 1 AND Deleted = 0
            ";


            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<EMTraining>(sql, sqlparam);
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 新增或更新訓練總記錄
        /// </summary>
        public async Task<bool> UpsertTrainingAsync(EMTraining training)
        {
            // 先檢查是否已存在
            var existing = await GetTrainingAsync(training.EmployeeMainID);

            if (existing != null)
            {
                // 更新現有記錄
                var sqlparam = new DynamicParameters();
                sqlparam.Add("ID", existing.ID);
                sqlparam.Add("Time", training.Time);
                sqlparam.Add("State", training.State);
                sqlparam.Add("ModifyDate", DateTime.Now);
                sqlparam.Add("ModifyUser", training.ModifyUser);
                sqlparam.Add("ModifyDept", training.ModifyDept);

                var sql = @"
                    UPDATE dbo.EMTraining
                    SET Time = @Time,
                        State = @State,
                        ModifyDate = @ModifyDate,
                        ModifyUser = @ModifyUser,
                        ModifyDept = @ModifyDept
                    WHERE ID = @ID
                ";


                using var conn = new SqlConnection(_dBList.erp);
                try
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // 新增記錄
                var sqlparam = new DynamicParameters();
                sqlparam.Add("ID", Guid.NewGuid());
                sqlparam.Add("EmployeeMainID", training.EmployeeMainID);
                sqlparam.Add("Time", training.Time);
                sqlparam.Add("State", training.State);
                sqlparam.Add("CreateDate", DateTime.Now);
                sqlparam.Add("CreateUser", training.CreateUser);
                sqlparam.Add("CreateDept", training.CreateDept);
                sqlparam.Add("ModifyDate", DateTime.Now);
                sqlparam.Add("ModifyUser", training.ModifyUser);
                sqlparam.Add("ModifyDept", training.ModifyDept);
                sqlparam.Add("Enabled", true);
                sqlparam.Add("Deleted", false);

                var sql = @"
                    INSERT INTO dbo.EMTraining 
                    (ID, EmployeeMainID, Time, State,
                     CreateDate, CreateUser, CreateDept, 
                     ModifyDate, ModifyUser, ModifyDept, 
                     Enabled, Deleted)
                    VALUES 
                    (@ID, @EmployeeMainID, @Time, @State,
                     @CreateDate, @CreateUser, @CreateDept, 
                     @ModifyDate, @ModifyUser, @ModifyDept, 
                     @Enabled, @Deleted)
                ";


                using var conn = new SqlConnection(_dBList.erp);
                try
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        #endregion
    }
}

