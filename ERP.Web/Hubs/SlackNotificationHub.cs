using Microsoft.AspNetCore.SignalR;

namespace ERP.Web.Hubs
{
    /// <summary>
    /// Slack 即時通知 Hub
    /// 用於推送 Slack 訊息通知給前端使用者
    /// </summary>
    public class SlackNotificationHub : Hub
    {
        /// <summary>
        /// 當使用者連接到 Hub 時
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 當使用者斷開連接時
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 加入使用者的 Slack 頻道群組（用於接收該頻道的通知）
        /// </summary>
        /// <param name="channelId">Slack 頻道 ID</param>
        public async Task JoinChannelGroup(string channelId)
        {
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"SlackChannel_{channelId}");
            }
        }

        /// <summary>
        /// 離開使用者的 Slack 頻道群組
        /// </summary>
        /// <param name="channelId">Slack 頻道 ID</param>
        public async Task LeaveChannelGroup(string channelId)
        {
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"SlackChannel_{channelId}");
            }
        }

        /// <summary>
        /// 加入使用者的 Slack 使用者群組（用於接收該使用者的所有頻道通知）
        /// </summary>
        /// <param name="userId">Slack 使用者 ID</param>
        public async Task JoinUserGroup(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"SlackUser_{userId}");
            }
        }

        /// <summary>
        /// 離開使用者的 Slack 使用者群組
        /// </summary>
        /// <param name="userId">Slack 使用者 ID</param>
        public async Task LeaveUserGroup(string userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"SlackUser_{userId}");
            }
        }
    }
}

