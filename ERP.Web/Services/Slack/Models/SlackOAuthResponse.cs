using System.Text.Json.Serialization;

namespace ERP.Web.Services.Slack.Models
{
    public class SlackOAuthResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("bot_user_id")]
        public string? BotUserId { get; set; }

        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }

        [JsonPropertyName("team")]
        public SlackTeamInfo? Team { get; set; }

        [JsonPropertyName("authed_user")]
        public SlackAuthedUser? AuthedUser { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackAuthedUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    public class SlackTeamInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
