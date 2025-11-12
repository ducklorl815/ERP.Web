using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Service.Service.Slack
{
    /// <summary>
    /// Slack OAuth 與 Bot 設定。
    /// </summary>
    public class SlackOptions
    {
        /// <summary>
        /// Slack App 的 Client Id。
        /// </summary>
        [Required]
        public string? ClientId { get; set; }

        /// <summary>
        /// Slack App 的 Client Secret，用於交換 access token。
        /// </summary>
        [Required]
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Slack App 的 Signing Secret，用於驗證回呼請求。
        /// </summary>
        [Required]
        public string? SigningSecret { get; set; }

        /// <summary>
        /// Slack OAuth Redirect URL。
        /// </summary>
        [Required]
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// Bot Token（xoxb-），若未透過 OAuth 換取，可先於設定檔提供測試用 token。
        /// </summary>
        public string? BotToken { get; set; }

        /// <summary>
        /// OAuth 所需的 scope，預設空字串避免 Null 例外。
        /// </summary>
        public string Scopes { get; set; } = string.Empty;

        /// <summary>
        /// OAuth user scope，預設空字串避免 Null 例外。
        /// </summary>
        public string UserScopes { get; set; } = string.Empty;

        /// <summary>
        /// 預設訊息發送頻道 Id，方便前端預先帶入。
        /// </summary>
        public string? DefaultChannel { get; set; }
    }
}
