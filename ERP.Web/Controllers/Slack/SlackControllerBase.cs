using ERP.Web.Models.Models.Slack;
using ERP.Web.Service.Service.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ERP.Web.Controllers.Slack
{
    /// <summary>
    /// Slack Controller 基礎類別，包含共享的常數、欄位和私有方法
    /// </summary>
    public abstract class SlackControllerBase : Controller
    {
        protected const string SessionBotTokenKey = "Slack:BotToken";
        protected const string SessionUserTokenKey = "Slack:UserToken";
        protected const string SessionUserIdKey = "Slack:UserId";

        // 緩存鍵常數
        protected const string ChannelsCacheKey = "Slack:Channels:Data";
        protected const string ChannelsCountCacheKey = "Slack:Channels:Count";
        protected const string ChannelsTypesCacheKey = "Slack:Channels:Types";
        protected const string UnreadMessagesCacheKeyPrefix = "Slack:UnreadMessages:";

        protected readonly ISlackService _slackService;
        protected readonly SlackOptions _options;
        protected readonly ILogger _logger;
        protected readonly IMemoryCache _memoryCache;
        protected readonly TimeSpan _channelsCacheDuration = TimeSpan.FromHours(12); // 頻道清單緩存 12 小時（最長保存時間）
        protected readonly TimeSpan _channelsCacheRefreshInterval = TimeSpan.FromHours(1); // 緩存刷新間隔 1 小時（1小時內直接使用緩存）

        protected SlackControllerBase(ISlackService slackService, IOptions<SlackOptions> options, ILogger logger, IMemoryCache memoryCache)
        {
            _slackService = slackService ?? throw new ArgumentNullException(nameof(slackService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// 取得有效的 Token（優先使用 UserToken，其次使用 BotToken，最後使用配置的 BotToken）
        /// </summary>
        protected string? GetEffectiveToken()
        {
            var userToken = HttpContext.Session.GetString(SessionUserTokenKey);
            if (!string.IsNullOrWhiteSpace(userToken))
            {
                return userToken;
            }

            var sessionToken = HttpContext.Session.GetString(SessionBotTokenKey);
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                return sessionToken;
            }

            return _options.BotToken;
        }

        /// <summary>
        /// 取得所有頻道清單（遍歷所有頁面）
        /// </summary>
        protected async Task<List<ERP.Web.Models.Models.Slack.SlackConversationItem>?> FetchAllChannelsAsync(string token, string types, CancellationToken cancellationToken)
        {
            var channels = new List<ERP.Web.Models.Models.Slack.SlackConversationItem>();
            var request = new ERP.Web.Models.Models.Slack.SlackConversationsListRequest
            {
                Types = types,
                ExcludeArchived = true,
                Limit = 1000 // 使用較大的 limit 減少 API 呼叫次數
            };

            do
            {
                var response = await _slackService.GetUserConversationsAsync(token, request, cancellationToken);

                if (response?.Ok != true)
                {
                    var error = response?.Error ?? "未知錯誤";
                    _logger.LogWarning("取得 Slack 頻道清單失敗：{Error}", error);
                    return channels.Count > 0 ? channels : null;
                }

                if (response.Channels?.Count > 0)
                {
                    channels.AddRange(response.Channels);
                }

                request.Cursor = response.ResponseMetadata?.NextCursor;

            } while (!string.IsNullOrWhiteSpace(request.Cursor));

            return channels;
        }

        /// <summary>
        /// 處理頻道清單，加入使用者資訊並轉換為回應格式
        /// </summary>
        protected async Task<List<ChannelCacheItem>> ProcessChannelsAsync(List<ERP.Web.Models.Models.Slack.SlackConversationItem> channels, string token, CancellationToken cancellationToken)
        {
            var userNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var realNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var imChannels = channels
                .Where(channel => channel?.IsIm == true && !string.IsNullOrWhiteSpace(channel.User))
                .Select(channel => channel!.User!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (imChannels.Count > 0)
            {
                var userInfos = await _slackService.GetUsersInfoAsync(token, imChannels, cancellationToken);

                foreach (var userId in imChannels)
                {
                    if (userNameCache.ContainsKey(userId))
                    {
                        continue;
                    }

                    if (userInfos.TryGetValue(userId, out var userInfo) && userInfo?.Ok == true)
                    {
                        var profile = userInfo.User?.Profile;
                        var displayName = profile?.DisplayName;

                        var realName = profile?.RealName;

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = profile?.DisplayNameNormalized;
                        }

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = profile?.RealName ?? profile?.RealNameNormalized;
                        }

                        // 如果 realName 為空，嘗試從其他來源取得
                        if (string.IsNullOrWhiteSpace(realName))
                        {
                            realName = userInfo.User?.RealName ?? userInfo.User?.Name;
                        }

                        userNameCache[userId] = string.IsNullOrWhiteSpace(displayName) ? userId : displayName!;
                        
                        // 儲存 realName 到快取中
                        if (!string.IsNullOrWhiteSpace(realName))
                        {
                            realNameCache[userId] = realName;
                        }
                    }
                    else
                    {
                        userNameCache[userId] = userId;
                    }
                }
            }

            var conversationItems = channels
                .Where(channel => channel != null && !string.IsNullOrWhiteSpace(channel.Id))
                .Select(channel =>
                {
                    var id = channel.Id ?? string.Empty;
                    var displayName = !string.IsNullOrWhiteSpace(channel.Name)
                        ? channel.Name
                        : !string.IsNullOrWhiteSpace(channel.User) && userNameCache.TryGetValue(channel.User, out var userDisplay)
                            ? userDisplay
                            : !string.IsNullOrWhiteSpace(channel.User) ? channel.User : id;
                    var userIdValue = channel.User ?? string.Empty;

                    // 從快取中取得 realName，如果快取中沒有，則使用 channel.RealName（作為後備）
                    var realName = !string.IsNullOrWhiteSpace(channel.User) && realNameCache.TryGetValue(channel.User, out var cachedRealName)
                        ? cachedRealName
                        : channel.RealName;

                    return new ChannelCacheItem
                    {
                        Id = id,
                        RealName = realName,
                        DisplayName = displayName,
                        IsIm = channel.IsIm,
                        IsPrivate = channel.IsPrivate,
                        IsMpim = channel.IsMpim,
                        MemberCount = channel.MemberCount,
                        UserId = userIdValue,
                        IsNew = false,
                        UnreadCount = null, // users.conversations API 不會返回未讀訊息數量
                        UnreadCountDisplay = null // 需要透過其他方式計算
                    };
                })
                .OrderBy(item => item.IsIm ? 0 : 1)
                .ThenBy(item => item.DisplayName)
                .ToList();

            return conversationItems;
        }

        /// <summary>
        /// 重新整理頻道緩存（非同步背景更新）
        /// </summary>
        protected async Task RefreshChannelsCacheAsync(string token, string types, CancellationToken cancellationToken)
        {
            try
            {
                var channels = await FetchAllChannelsAsync(token, types, cancellationToken);
                if (channels == null || channels.Count == 0)
                {
                    _logger.LogWarning("無法取得頻道清單，無法更新緩存");
                    return;
                }

                var processedChannels = await ProcessChannelsAsync(channels, token, cancellationToken);
                var cacheKey = $"{ChannelsCacheKey}:{types}";
                var countCacheKey = $"{ChannelsCountCacheKey}:{types}";
                var now = DateTime.UtcNow;

                // 創建 ChannelsCacheWrapper 並保存
                var cacheWrapper = new ChannelsCacheWrapper
                {
                    Channels = processedChannels,
                    LastUpdated = now
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _channelsCacheDuration,
                    SlidingExpiration = null
                };

                _memoryCache.Set(cacheKey, cacheWrapper, cacheOptions);

                // 更新頻道數量緩存
                var channelCount = await _slackService.GetUserConversationsCountAsync(token, types, cancellationToken);
                if (channelCount.HasValue)
                {
                    _memoryCache.Set(countCacheKey, channelCount.Value, cacheOptions);
                    _logger.LogInformation("頻道緩存已更新，數量：{Count}，更新時間：{LastUpdated}", channelCount.Value, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新頻道緩存失敗");
            }
        }

        /// <summary>
        /// 頻道緩存項目（用於伺服器端緩存）
        /// </summary>
        protected sealed class ChannelCacheItem
        {
            public string Id { get; set; } = string.Empty;
            public string? RealName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool IsIm { get; set; }
            public bool IsPrivate { get; set; }
            public bool IsMpim { get; set; }
            public int? MemberCount { get; set; }
            public string UserId { get; set; } = string.Empty;
            public bool IsNew { get; set; }
            public int? UnreadCount { get; set; }
            public int? UnreadCountDisplay { get; set; }
        }

        /// <summary>
        /// 頻道緩存包裝類（包含數據和時間戳）
        /// </summary>
        protected sealed class ChannelsCacheWrapper
        {
            public List<ChannelCacheItem> Channels { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        }
    }
}

