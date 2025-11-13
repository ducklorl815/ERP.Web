using ERP.Web.Models.Models.Slack;

namespace ERP.Web.Service.Service.Slack
{
    public interface ISlackService
    {
        /// <summary>
        /// 透過 OAuth 授權碼向 Slack 交換 access token。
        /// </summary>
        Task<SlackOAuthResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用指定的 token 發送訊息到 Slack。
        /// </summary>
        Task<SlackPostMessageResponse?> PostMessageAsync(string token, SlackPostMessageRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證 Slack 送出的簽章，確保請求未被竄改。
        /// </summary>
        bool ValidateSignature(string requestTimestamp, string signature, string requestBody);

        /// <summary>
        /// 取得頻道歷史訊息，方便之後在系統內顯示聊天內容。
        /// </summary>
        Task<SlackConversationsHistoryResponse?> GetChannelMessagesAsync(string token, string channel, int limit = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得目前使用者可存取的 Slack 對話清單。
        /// </summary>
        Task<SlackConversationsListResponse?> GetUserConversationsAsync(string token, SlackConversationsListRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得目前使用者可存取的 Slack 對話數量（僅檢查數量，不獲取完整資料）。
        /// </summary>
        Task<int?> GetUserConversationsCountAsync(string token, string types = "private_channel,im", CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得 Slack 使用者資訊（顯示名稱、真實姓名等）。
        /// </summary>
        Task<SlackUserInfoResponse?> GetUserInfoAsync(string token, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批次取得 Slack 使用者資訊，降低 API 呼叫次數。
        /// </summary>
        Task<IDictionary<string, SlackUserInfoResponse>> GetUsersInfoAsync(string token, IEnumerable<string> userIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 依關鍵字搜尋 Slack 使用者。
        /// </summary>
        Task<IReadOnlyCollection<SlackUser>> SearchUsersAsync(string token, string keyword, int limit, CancellationToken cancellationToken = default);

        /// <summary>
        /// 開啟與指定使用者或多人對話，回傳對應的頻道資訊。
        /// </summary>
        Task<SlackConversationOpenResponse?> OpenConversationAsync(string token, IEnumerable<string> userIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得最近溝通的私人頻道清單（按最後更新時間排序）。
        /// </summary>
        Task<SlackConversationsListResponse?> GetRecentConversationsAsync(string token, string types = "im,mpim", int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得頻道資訊（用於確定直接訊息的接收者）。
        /// </summary>
        Task<SlackConversationInfoResponse?> GetConversationInfoAsync(string token, string channelId, CancellationToken cancellationToken = default);
    }
}
