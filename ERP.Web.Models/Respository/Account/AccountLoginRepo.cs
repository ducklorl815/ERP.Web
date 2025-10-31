using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ERP.Web.Models.Respository.Account
{
    /// <summary>
    /// 帳號登入 Repository
    /// 負責所有與登入相關的資料庫操作
    /// </summary>
    public class AccountLoginRepo
    {
        private readonly string _connectionString;

        public AccountLoginRepo(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        /// <summary>
        /// 驗證使用者帳號密碼
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>驗證結果（true: 成功, false: 失敗）</returns>
        public async Task<bool> ValidateUserAsync(string account, string password)
        {
            // TODO: 實作資料庫驗證邏輯
            // 
            // 建議 SQL 範例：
            // SELECT COUNT(*) FROM Users 
            // WHERE Account = @Account 
            //   AND Password = HASHBYTES('SHA2_256', @Password + Salt)
            //   AND IsActive = 1
            //
            // 注意：
            // 1. 密碼應該使用 Hash + Salt 儲存，不應明文比對
            // 2. 檢查帳號是否啟用（IsActive）
            // 3. 可以記錄登入嘗試次數，防止暴力破解

            // 目前回傳假資料（供測試用）
            await Task.Delay(100); // 模擬資料庫查詢延遲
            return true; // 假設驗證成功
        }

        /// <summary>
        /// 根據帳號取得使用者完整資料
        /// </summary>
        /// <param name="account">帳號</param>
        /// <returns>使用者資料（Account, EmpName, EmpID 等）</returns>
        public async Task<UserData?> GetUserDataAsync(string account)
        {
            // TODO: 實作從資料庫取得使用者資料的邏輯
            //
            // 建議 SQL 範例：
            // SELECT 
            //     Account,
            //     EmpID,
            //     EmpName,
            //     Department,
            //     Email,
            //     IsActive,
            //     LastLoginTime
            // FROM Users
            // WHERE Account = @Account
            //   AND IsActive = 1

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // TODO: 撰寫 SQL 查詢
                    // 目前回傳假資料
                    await Task.Delay(100); // 模擬資料庫查詢延遲

                    // 假資料（供測試用）
                    return new UserData
                    {
                        Account = account,
                        EmpID = "E001",
                        EmpName = "測試使用者",
                        Department = "資訊部",
                        Email = $"{account}@company.com",
                        IsActive = true,
                        LastLoginTime = DateTime.Now.AddDays(-1)
                    };
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"取得使用者資料時發生錯誤: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新最後登入時間
        /// </summary>
        /// <param name="account">帳號</param>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdateLastLoginTimeAsync(string account)
        {
            // TODO: 實作更新最後登入時間的邏輯
            //
            // 建議 SQL 範例：
            // UPDATE Users 
            // SET LastLoginTime = GETDATE(),
            //     LoginCount = LoginCount + 1
            // WHERE Account = @Account

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // TODO: 撰寫 SQL 更新語句
                    await Task.Delay(50); // 模擬資料庫更新延遲

                    return true; // 假設更新成功
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新登入時間時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 記錄登入失敗
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="reason">失敗原因</param>
        /// <returns>是否記錄成功</returns>
        public async Task<bool> LogFailedLoginAsync(string account, string reason)
        {
            // TODO: 實作記錄登入失敗的邏輯
            //
            // 建議 SQL 範例：
            // INSERT INTO LoginFailedLog (Account, Reason, AttemptTime, IPAddress)
            // VALUES (@Account, @Reason, GETDATE(), @IPAddress)
            //
            // 可以實作：
            // 1. 記錄失敗次數
            // 2. 超過次數後鎖定帳號
            // 3. 記錄 IP 位址

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // TODO: 撰寫 SQL 插入語句
                    await Task.Delay(50); // 模擬資料庫插入延遲

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"記錄登入失敗時發生錯誤: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 使用者資料模型
    /// </summary>
    public class UserData
    {
        /// <summary>使用者帳號</summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>員工ID</summary>
        public string EmpID { get; set; } = string.Empty;

        /// <summary>員工姓名</summary>
        public string EmpName { get; set; } = string.Empty;

        /// <summary>部門</summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>Email</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>是否啟用</summary>
        public bool IsActive { get; set; }

        /// <summary>最後登入時間</summary>
        public DateTime? LastLoginTime { get; set; }
    }
}

