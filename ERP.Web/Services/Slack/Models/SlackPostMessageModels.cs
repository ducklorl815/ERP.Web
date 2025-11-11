using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ERP.Web.Services.Slack.Models
{
    public class SlackPostMessageRequest
    {
        [Required]
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [Required]
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class SlackPostMessageResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("ts")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackConversationsHistoryResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("messages")]
        public List<SlackMessageItem> Messages { get; set; } = new();

        [JsonPropertyName("response_metadata")]
        public SlackResponseMetadata? ResponseMetadata { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackMessageItem
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("ts")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

        [JsonPropertyName("bot_id")]
        public string? BotId { get; set; }
    }

    public class SlackResponseMetadata
    {
        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }
    }

    public class SlackUserInfoResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("user")]
        public SlackUser? User { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("real_name")]
        public string? RealName { get; set; }

        [JsonPropertyName("profile")]
        public SlackUserProfile? Profile { get; set; }
    }

    public class SlackUserProfile
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("real_name")]
        public string? RealName { get; set; }

        [JsonPropertyName("display_name_normalized")]
        public string? DisplayNameNormalized { get; set; }

        [JsonPropertyName("real_name_normalized")]
        public string? RealNameNormalized { get; set; }
    }

    public class SlackUsersListResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("members")]
        public List<SlackUser> Members { get; set; } = new();

        [JsonPropertyName("response_metadata")]
        public SlackResponseMetadata? ResponseMetadata { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackConversationOpenResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("channel")]
        public SlackConversationItem? Channel { get; set; }

        [JsonPropertyName("no_op")]
        public bool NoOp { get; set; }

        [JsonPropertyName("already_open")]
        public bool AlreadyOpen { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackConversationsListResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("channels")]
        public List<SlackConversationItem> Channels { get; set; } = new();

        [JsonPropertyName("response_metadata")]
        public SlackResponseMetadata? ResponseMetadata { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SlackConversationItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("is_im")]
        public bool IsIm { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("is_mpim")]
        public bool IsMpim { get; set; }

        [JsonPropertyName("is_channel")]
        public bool IsChannel { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("num_members")]
        public int? MemberCount { get; set; }
    }

    public class SlackConversationsListRequest
    {
        public string Types { get; set; } = "public_channel,private_channel,im";

        public bool ExcludeArchived { get; set; } = true;

        public int Limit { get; set; } = 200;

        public string? Cursor { get; set; }
    }
}
