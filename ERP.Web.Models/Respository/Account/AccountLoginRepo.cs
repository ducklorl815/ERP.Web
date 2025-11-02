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
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>驗證結果（true: 成功, false: 失敗）</returns>
        public async Task<bool> ValidateUserAsync(string account, string password)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);
            sqlparam.Add("Password", password);

            var sql = @"
                    SELECT COUNT(*) 
                    FROM EmployeeMain
                    WHERE Email = @Account
                      AND Password = HASHBYTES('SHA2_256', @Password + Salt)
                      AND Enabled = 1
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

                throw;
            }
        }

        /// <summary>
        /// 根據帳號取得使用者完整資料
        /// </summary>
        /// <param name="account">帳號</param>
        /// <returns>使用者資料（Account, EmpName, EmpID 等）</returns>
        public async Task<EmployeeMain> GetUserDataAsync(string account)
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
                        FROM erp.dbo.EmployeeMain
                        WHERE Email = @Email
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

                throw;
            }
        }

        /// <summary>
        /// 更新最後登入時間
        /// </summary>
        /// <param name="account">帳號</param>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdateLastLoginTimeAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                        UPDATE erp.dbo.EmployeeMain
                           SET LastLoginTime = GETDATE()
                           WHERE Email = @Email
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
                throw;
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
                using (var connection = new SqlConnection(_dBList.erp))
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
    public class EmployeeMain
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
        public DateTime LastLoginTime { get; set; }
    }
}

