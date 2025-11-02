using Dapper;
using ERP.Web.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ERP.Web.Models.Respository.Account
{
    /// <summary>
    /// 帳號登入 Repository
    /// 負責所有與登入相關的資料庫操作
    /// </summary>
    public class AccountLoginRepo
    {
        private readonly DBList _dBList;
        public AccountLoginRepo
            (
             IOptions<DBList> dBList,
             IConfiguration configuration
            )
        {
            _dBList = dBList.Value;
        }
        /// <summary>
        /// 驗證使用者帳號密碼
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <param name="password">密碼（明文）</param>
        /// <returns>驗證結果（true: 成功, false: 失敗）</returns>
        public async Task<bool> ValidateUserAsync(string account, string password)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);
            sqlparam.Add("Password", password);

            var sql = @"
                    SELECT COUNT(*) 
                      FROM erp.dbo.EmployeeMain
                     WHERE Email = @Email
                       AND Password = HASHBYTES('SHA2_256', @Password + Salt)
                       AND IsActive = 1
                       AND Enabled = 1
                       AND Deleted = 0
                       ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteScalarAsync<int>(sql, sqlparam);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"驗證使用者時發生錯誤: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根據帳號取得使用者完整資料
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <returns>使用者資料（ID, CName, Email 等），若找不到則回傳 null</returns>
        public async Task<EmployeeMain?> GetUserDataAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                        SELECT ID
                              ,CName
                              ,FirstEName
                              ,LastEName
                              ,Password
                              ,Email
                              ,Phone
                              ,Sex
                              ,IsActive
                              ,LastLoginTime
                              ,ModifyDate
                              ,ModifyUser
                              ,Enabled
                              ,Deleted
                          FROM erp.dbo.EmployeeMain
                         WHERE Email = @Email
                           AND IsActive = 1
                           AND Enabled = 1
                           AND Deleted = 0
                        ";
            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<EmployeeMain>(sql, sqlparam);
                    return result;
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"取得使用者資料時發生錯誤: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新最後登入時間
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdateLastLoginTimeAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                        UPDATE erp.dbo.EmployeeMain
                           SET LastLoginTime = GETDATE()
                         WHERE Email = @Email
                           AND IsActive = 1
                           AND Enabled = 1
                           AND Deleted = 0
                        ";
            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"更新最後登入時間時發生錯誤: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// 記錄登入失敗
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <param name="reason">失敗原因</param>
        /// <param name="ipAddress">IP 位址</param>
        /// <returns>是否記錄成功</returns>
        public async Task<bool> LogFailedLoginAsync(string account, string reason, string ipAddress)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);
            sqlparam.Add("Reason", reason);
            sqlparam.Add("IPAddress", ipAddress ?? "Unknown");

            var sql = @"
                    INSERT INTO erp.dbo.EmployeeLoginFailedLog
                           (Email
                           ,Reason
                           ,AttemptTime
                           ,IPAddress
                           ,ModifyDate
                           ,Enabled
                           ,Deleted)
                     VALUES
                           (@Email
                           ,@Reason
                           ,GETDATE()
                           ,@IPAddress
                           ,GETDATE()
                           ,1
                           ,0)
                       ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤（記錄失敗不應影響登入流程）
                Console.WriteLine($"記錄登入失敗時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取得使用者的登入失敗次數（24小時內）
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <returns>失敗次數</returns>
        public async Task<int> GetFailedLoginCountAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                    SELECT COUNT(*)
                      FROM erp.dbo.EmployeeLoginFailedLog
                     WHERE Email = @Email
                       AND AttemptTime >= DATEADD(HOUR, -24, GETDATE())
                       AND Enabled = 1
                       AND Deleted = 0
                       ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteScalarAsync<int>(sql, sqlparam);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得登入失敗次數時發生錯誤: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 清除登入失敗記錄（登入成功時調用）
        /// </summary>
        /// <param name="account">帳號（Email）</param>
        /// <returns>是否清除成功</returns>
        public async Task<bool> ClearFailedLoginAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                    UPDATE erp.dbo.EmployeeLoginFailedLog
                       SET Deleted = 1
                          ,ModifyDate = GETDATE()
                     WHERE Email = @Email
                       AND Deleted = 0
                       ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result >= 0; // 即使沒有記錄也算成功
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除登入失敗記錄時發生錯誤: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 使用者資料模型（對應資料表：erp.dbo.EmployeeMain）
    /// </summary>
    public class EmployeeMain
    {
        /// <summary>員工ID（主鍵）</summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>中文姓名</summary>
        public string CName { get; set; } = string.Empty;

        /// <summary>英文名字</summary>
        public string FirstEName { get; set; } = string.Empty;

        /// <summary>英文姓氏</summary>
        public string LastEName { get; set; } = string.Empty;

        /// <summary>密碼（雜湊值）</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Email（作為帳號使用）</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>電話</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>性別</summary>
        public string Sex { get; set; } = string.Empty;

        /// <summary>是否啟用（業務邏輯欄位）</summary>
        public bool IsActive { get; set; }

        /// <summary>最後登入時間</summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>修改日期</summary>
        public DateTime? ModifyDate { get; set; }

        /// <summary>修改人員</summary>
        public string ModifyUser { get; set; } = string.Empty;

        /// <summary>啟用狀態（系統欄位）</summary>
        public bool Enabled { get; set; }

        /// <summary>刪除標記</summary>
        public bool Deleted { get; set; }

        // === 以下為方便使用的屬性對應 ===

        /// <summary>帳號（對應到 Email）</summary>
        public string Account => Email;

        /// <summary>員工ID（對應到 ID）</summary>
        public string EmpID => ID;

        /// <summary>員工姓名（對應到 CName）</summary>
        public string EmpName => CName;
    }

    /// <summary>
    /// 員工登入失敗記錄模型（對應資料表：erp.dbo.EmployeeLoginFailedLog）
    /// </summary>
    public class EmployeeLoginFailedLog
    {
        /// <summary>記錄ID（主鍵）</summary>
        public int ID { get; set; }

        /// <summary>Email（帳號）</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>失敗原因</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>嘗試時間</summary>
        public DateTime AttemptTime { get; set; }

        /// <summary>IP 位址</summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>修改日期</summary>
        public DateTime? ModifyDate { get; set; }

        /// <summary>修改人員</summary>
        public string ModifyUser { get; set; } = string.Empty;

        /// <summary>啟用狀態</summary>
        public bool Enabled { get; set; }

        /// <summary>刪除標記</summary>
        public bool Deleted { get; set; }
    }
}

