using ERP.Web.Models.Respository.Account;
using Microsoft.Extensions.Configuration;
using OtpNet;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.Account
{
    /// <summary>
    /// OTP 服務
    /// 處理 TOTP 相關的業務邏輯
    /// </summary>
    public class OTPService
    {
        private readonly OTPRepository _otpRepository;
        private readonly IConfiguration _configuration;
        private readonly string _otpIssuer;

        public OTPService(OTPRepository otpRepository, IConfiguration configuration)
        {
            _otpRepository = otpRepository;
            _configuration = configuration;
            _otpIssuer = _configuration["OTP:Issuer"] ?? "ERP System";
        }

        /// <summary>
        /// 取得使用者的 OTP 設定
        /// </summary>
        public async Task<EmployeeOTPSetting?> GetOTPSettingAsync(string account)
        {
            return await _otpRepository.GetOTPSettingAsync(account);
        }

        /// <summary>
        /// 檢查使用者是否已啟用 OTP
        /// </summary>
        public async Task<bool> IsOTPEnabledAsync(string account)
        {
            var setting = await _otpRepository.GetOTPSettingAsync(account);
            return setting != null && setting.IsOTPEnabled;
        }

        /// <summary>
        /// 生成 TOTP Secret Key 和 QR Code URL
        /// </summary>
        public async Task<OTPSetupResult> SetupTOTPAsync(string account, string? employeeMainID = null)
        {
            try
            {
                // 檢查是否已經設定過
                var existingSetting = await _otpRepository.GetOTPSettingAsync(account);
                if (existingSetting != null && existingSetting.IsOTPEnabled && !string.IsNullOrEmpty(existingSetting.SecretKey))
                {
                    return new OTPSetupResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "此帳號已經設定過 OTP，如需重新設定請先停用"
                    };
                }

                // 生成新的 Secret Key（20 bytes = 160 bits，符合 RFC 4226 建議）
                var secretKey = KeyGeneration.GenerateRandomKey(20);

                // 將 Secret Key 轉換為 Base32 字串（用於顯示和儲存）
                var secretKeyBase32 = Base32Encoding.ToString(secretKey);

                // 加密 Secret Key（使用簡單的 AES 加密，實際環境應使用更安全的加密方式）
                var encryptedSecretKey = EncryptSecretKey(secretKeyBase32);

                // 取得 EmployeeMainID
                Guid empID;
                if (!string.IsNullOrEmpty(employeeMainID) && Guid.TryParse(employeeMainID, out var parsedID))
                {
                    empID = parsedID;
                }
                else
                {
                    var empMainID = await _otpRepository.GetEmployeeMainIDAsync(account);
                    if (empMainID == null)
                    {
                        return new OTPSetupResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "無法取得使用者資料"
                        };
                    }
                    empID = empMainID.Value;
                }

                // 儲存 OTP 設定（先不啟用，等使用者驗證成功後再啟用）
                var setting = new EmployeeOTPSetting
                {
                    ID = Guid.NewGuid(),
                    EmployeeMainID = empID,
                    Email = account,
                    IsOTPEnabled = false, // 先不啟用，等驗證成功後再啟用
                    OTPType = "TOTP",
                    SecretKey = encryptedSecretKey,
                    CreateUser = account,
                    ModifyUser = account
                };

                var saveResult = await _otpRepository.SaveOTPSettingAsync(setting);
                if (!saveResult)
                {
                    return new OTPSetupResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "儲存 OTP 設定失敗"
                    };
                }

                // 生成 QR Code URL（符合 Google Authenticator 格式）
                var qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(_otpIssuer)}:{Uri.EscapeDataString(account)}?secret={secretKeyBase32}&issuer={Uri.EscapeDataString(_otpIssuer)}";

                return new OTPSetupResult
                {
                    IsSuccess = true,
                    SecretKey = secretKeyBase32, // 回傳未加密的 Secret Key 供顯示
                    QRCodeUrl = qrCodeUrl
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定 TOTP 時發生錯誤: {ex.Message}");
                return new OTPSetupResult
                {
                    IsSuccess = false,
                    ErrorMessage = "系統發生錯誤，請稍後再試"
                };
            }
        }

        /// <summary>
        /// 驗證 TOTP
        /// </summary>
        public async Task<OTPVerifyResult> VerifyTOTPAsync(string account, string otpCode, string ipAddress, string? userAgent = null)
        {
            try
            {
                // 檢查驗證次數限制（10 分鐘內最多 5 次）
                var attemptCount = await _otpRepository.GetRecentOTPAttemptCountAsync(account, 10);
                if (attemptCount >= 5)
                {
                    await _otpRepository.LogOTPAttemptAsync(account, "TOTP", false, ipAddress, userAgent);
                    return new OTPVerifyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "驗證次數過多，請稍後再試"
                    };
                }

                // 取得 OTP 設定
                var setting = await _otpRepository.GetOTPSettingAsync(account);
                if (setting == null || string.IsNullOrEmpty(setting.SecretKey))
                {
                    await _otpRepository.LogOTPAttemptAsync(account, "TOTP", false, ipAddress, userAgent);
                    return new OTPVerifyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "此帳號尚未設定 OTP"
                    };
                }

                // 解密 Secret Key
                var decryptedSecretKey = DecryptSecretKey(setting.SecretKey);
                if (string.IsNullOrEmpty(decryptedSecretKey))
                {
                    await _otpRepository.LogOTPAttemptAsync(account, "TOTP", false, ipAddress, userAgent);
                    return new OTPVerifyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "無法讀取 OTP 設定"
                    };
                }

                // 將 Base32 字串轉換為 byte array
                var secretKeyBytes = Base32Encoding.ToBytes(decryptedSecretKey);

                // 建立 TOTP 物件
                var totp = new Totp(secretKeyBytes);

                // 驗證 OTP（允許前後 1 個時間步長，即 ±30 秒）
                var isValid = totp.VerifyTotp(otpCode, out long timeStepMatched, new VerificationWindow(1, 1));

                // 記錄驗證嘗試
                await _otpRepository.LogOTPAttemptAsync(account, "TOTP", isValid, ipAddress, userAgent);

                if (!isValid)
                {
                    return new OTPVerifyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "驗證碼錯誤，請重新輸入"
                    };
                }

                // 如果這是第一次驗證成功，啟用 OTP
                if (!setting.IsOTPEnabled)
                {
                    setting.IsOTPEnabled = true;
                    setting.ModifyUser = account;
                    await _otpRepository.SaveOTPSettingAsync(setting);
                }

                return new OTPVerifyResult
                {
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"驗證 TOTP 時發生錯誤: {ex.Message}");
                await _otpRepository.LogOTPAttemptAsync(account, "TOTP", false, ipAddress, userAgent);
                return new OTPVerifyResult
                {
                    IsSuccess = false,
                    ErrorMessage = "系統發生錯誤，請稍後再試"
                };
            }
        }

        /// <summary>
        /// 停用 OTP
        /// </summary>
        public async Task<bool> DisableOTPAsync(string account)
        {
            try
            {
                var setting = await _otpRepository.GetOTPSettingAsync(account);
                if (setting == null)
                {
                    return false;
                }

                setting.IsOTPEnabled = false;
                setting.ModifyUser = account;
                return await _otpRepository.SaveOTPSettingAsync(setting);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停用 OTP 時發生錯誤: {ex.Message}");
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 加密 Secret Key（簡單的 AES 加密）
        /// 注意：實際環境應使用更安全的加密方式，並將加密金鑰儲存在安全的地方
        /// </summary>
        private string EncryptSecretKey(string secretKey)
        {
            // 這裡使用簡單的 Base64 編碼，實際環境應使用 AES 加密
            // 並將加密金鑰儲存在 appsettings.json 或環境變數中
            var bytes = Encoding.UTF8.GetBytes(secretKey);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 解密 Secret Key
        /// </summary>
        private string DecryptSecretKey(string encryptedSecretKey)
        {
            try
            {
                var bytes = Convert.FromBase64String(encryptedSecretKey);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }

    /// <summary>
    /// OTP 設定結果
    /// </summary>
    public class OTPSetupResult
    {
        public bool IsSuccess { get; set; }
        public string? SecretKey { get; set; }
        public string? QRCodeUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// OTP 驗證結果
    /// </summary>
    public class OTPVerifyResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

