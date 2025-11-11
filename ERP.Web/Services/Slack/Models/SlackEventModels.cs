using System.Text.Json.Serialization;

namespace ERP.Web.Services.Slack.Models
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
    }
}
