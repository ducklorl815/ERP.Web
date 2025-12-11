using Dapper;
using ERP.Web.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Models.Respository.Account
{
    /// <summary>
    /// OTP Repository
    /// 處理所有與 OTP 相關的資料庫操作
    /// </summary>
    public class OTPRepository
    {
        private readonly DBList _dBList;

        public OTPRepository(IOptions<DBList> dBList)
        {
            _dBList = dBList.Value;
        }

        /// <summary>
        /// 取得使用者的 OTP 設定
        /// </summary>
        public async Task<EmployeeOTPSetting?> GetOTPSettingAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                SELECT ID
                      ,EmployeeMainID
                      ,Email
                      ,IsOTPEnabled
                      ,OTPType
                      ,SecretKey
                      ,BackupPhone
                      ,BackupEmail
                      ,CreateDate
                      ,CreateUser
                      ,ModifyDate
                      ,ModifyUser
                      ,Enabled
                      ,Deleted
                  FROM erp.dbo.EmployeeOTPSetting
                 WHERE Email = @Email
                   AND Enabled = 1
                   AND Deleted = 0
            ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<EmployeeOTPSetting>(sql, sqlparam);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得 OTP 設定時發生錯誤: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 儲存 OTP 設定
        /// </summary>
        public async Task<bool> SaveOTPSettingAsync(EmployeeOTPSetting setting)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", setting.ID);
            sqlparam.Add("EmployeeMainID", setting.EmployeeMainID);
            sqlparam.Add("Email", setting.Email);
            sqlparam.Add("IsOTPEnabled", setting.IsOTPEnabled);
            sqlparam.Add("OTPType", setting.OTPType ?? "TOTP");
            sqlparam.Add("SecretKey", setting.SecretKey); // 已加密的 Secret Key
            sqlparam.Add("BackupPhone", setting.BackupPhone);
            sqlparam.Add("BackupEmail", setting.BackupEmail);
            sqlparam.Add("CreateUser", setting.CreateUser);
            sqlparam.Add("ModifyUser", setting.ModifyUser);

            var sql = @"
                IF EXISTS (SELECT 1 FROM erp.dbo.EmployeeOTPSetting WHERE Email = @Email AND Deleted = 0)
                BEGIN
                    UPDATE erp.dbo.EmployeeOTPSetting
                       SET IsOTPEnabled = @IsOTPEnabled
                          ,OTPType = @OTPType
                          ,SecretKey = @SecretKey
                          ,BackupPhone = @BackupPhone
                          ,BackupEmail = @BackupEmail
                          ,ModifyDate = GETDATE()
                          ,ModifyUser = @ModifyUser
                     WHERE Email = @Email
                       AND Deleted = 0
                END
                ELSE
                BEGIN
                    INSERT INTO erp.dbo.EmployeeOTPSetting
                           (ID
                           ,EmployeeMainID
                           ,Email
                           ,IsOTPEnabled
                           ,OTPType
                           ,SecretKey
                           ,BackupPhone
                           ,BackupEmail
                           ,CreateDate
                           ,CreateUser
                           ,ModifyDate
                           ,ModifyUser
                           ,Enabled
                           ,Deleted)
                     VALUES
                           (@ID
                           ,@EmployeeMainID
                           ,@Email
                           ,@IsOTPEnabled
                           ,@OTPType
                           ,@SecretKey
                           ,@BackupPhone
                           ,@BackupEmail
                           ,GETDATE()
                           ,@CreateUser
                           ,GETDATE()
                           ,@ModifyUser
                           ,1
                           ,0)
                END
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
                Console.WriteLine($"儲存 OTP 設定時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 記錄 OTP 驗證嘗試
        /// </summary>
        public async Task<bool> LogOTPAttemptAsync(string account, string otpType, bool isSuccess, string ipAddress, string? userAgent = null)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);
            sqlparam.Add("OTPType", otpType);
            sqlparam.Add("IsVerified", isSuccess);
            sqlparam.Add("IPAddress", ipAddress ?? "Unknown");
            sqlparam.Add("UserAgent", userAgent ?? string.Empty);

            var sql = @"
                INSERT INTO erp.dbo.EmployeeOTPLog
                       (ID
                       ,Email
                       ,OTPType
                       ,IsVerified
                       ,VerifiedTime
                       ,IPAddress
                       ,UserAgent
                       ,CreateDate
                       ,Enabled
                       ,Deleted)
                 VALUES
                       (NEWID()
                       ,@Email
                       ,@OTPType
                       ,@IsVerified
                       ,CASE WHEN @IsVerified = 1 THEN GETDATE() ELSE NULL END
                       ,@IPAddress
                       ,@UserAgent
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
                Console.WriteLine($"記錄 OTP 驗證嘗試時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取得使用者最近的 OTP 驗證記錄（用於檢查驗證次數限制）
        /// </summary>
        public async Task<int> GetRecentOTPAttemptCountAsync(string account, int minutes = 10)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);
            sqlparam.Add("Minutes", minutes);

            var sql = @"
                SELECT COUNT(*)
                  FROM erp.dbo.EmployeeOTPLog
                 WHERE Email = @Email
                   AND CreateDate >= DATEADD(MINUTE, -@Minutes, GETDATE())
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
                Console.WriteLine($"取得 OTP 驗證次數時發生錯誤: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 取得使用者的 EmployeeMainID（用於建立 OTP 設定）
        /// </summary>
        public async Task<Guid?> GetEmployeeMainIDAsync(string account)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Email", account);

            var sql = @"
                SELECT CAST(ID AS UNIQUEIDENTIFIER)
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
                    var result = await conn.ExecuteScalarAsync<Guid?>(sql, sqlparam);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得 EmployeeMainID 時發生錯誤: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 員工 OTP 設定模型（對應資料表：erp.dbo.EmployeeOTPSetting）
    /// </summary>
    public class EmployeeOTPSetting
    {
        public Guid ID { get; set; }
        public Guid EmployeeMainID { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsOTPEnabled { get; set; }
        public string? OTPType { get; set; } // TOTP, SMS, Email
        public string? SecretKey { get; set; } // 加密儲存的 Secret Key
        public string? BackupPhone { get; set; }
        public string? BackupEmail { get; set; }
        public DateTime CreateDate { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? ModifyUser { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 員工 OTP 驗證記錄模型（對應資料表：erp.dbo.EmployeeOTPLog）
    /// </summary>
    public class EmployeeOTPLog
    {
        public Guid ID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OTPType { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime? VerifiedTime { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }
}

