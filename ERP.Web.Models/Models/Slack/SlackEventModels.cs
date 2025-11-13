using System.Text.Json.Serialization;

namespace ERP.Web.Models.Models.Slack
{
    public class SlackEventEnvelope
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("challenge")]
        public string? Challenge { get; set; }

        [JsonPropertyName("event")]
        public SlackEventBody? Event { get; set; }

        [JsonPropertyName("team_id")]
        public string? TeamId { get; set; }

        [JsonPropertyName("api_app_id")]
        public string? ApiAppId { get; set; }
    }

    public class SlackEventBody
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("ts")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("bot_id")]
        public string? BotId { get; set; }

        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

        /// <summary>
        /// 頻道類型（im = 直接訊息, mpim = 多人直接訊息, channel = 公開頻道, group = 私人頻道）
        /// </summary>
        [JsonPropertyName("channel_type")]
        public string? ChannelType { get; set; }
    }

    /// <summary>
    /// Slack 未讀訊息通知模型
    /// </summary>
    public class SlackUnreadMessage
    {
        /// <summary>
        /// 頻道 ID
        /// </summary>
        public string ChannelId { get; set; } = string.Empty;

        /// <summary>
        /// 發送者使用者 ID
        /// </summary>
        public string? SenderUserId { get; set; }

        /// <summary>
        /// 發送者顯示名稱
        /// </summary>
        public string? SenderDisplayName { get; set; }

        /// <summary>
        /// 訊息內容
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// 訊息時間戳記
        /// </summary>
        public string? Timestamp { get; set; }

        /// <summary>
        /// 接收時間（伺服器時間）
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否已讀
        /// </summary>
        public bool IsRead { get; set; }
    }
}
