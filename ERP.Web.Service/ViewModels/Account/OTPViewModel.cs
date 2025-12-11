namespace ERP.Web.Service.ViewModels.Account
{
    /// <summary>
    /// OTP 驗證請求 ViewModel
    /// </summary>
    public class OTPVerifyViewModel
    {
        /// <summary>
        /// 帳號
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// OTP 驗證碼
        /// </summary>
        public string OTPCode { get; set; } = string.Empty;

        /// <summary>
        /// 返回 URL
        /// </summary>
        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// OTP 設定 ViewModel
    /// </summary>
    public class OTPSetupViewModel
    {
        /// <summary>
        /// 帳號
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// Secret Key（Base32 格式）
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>
        /// QR Code URL
        /// </summary>
        public string? QRCodeUrl { get; set; }

        /// <summary>
        /// 是否已啟用 OTP
        /// </summary>
        public bool IsOTPEnabled { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

