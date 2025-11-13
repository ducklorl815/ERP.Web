using ERP.Web.Models.Models.Slack;
using ERP.Web.Service.Service.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ERP.Web.Controllers.Slack
{
    [Route("slack")]
    public class SlackController : Controller
    {
        private const string SessionBotTokenKey = "Slack:BotToken";
        private const string SessionUserTokenKey = "Slack:UserToken";
        private const string SessionUserIdKey = "Slack:UserId";

        // 緩存鍵常數
        private const string ChannelsCacheKey = "Slack:Channels:Data";
        private const string ChannelsCountCacheKey = "Slack:Channels:Count";
        private const string ChannelsTypesCacheKey = "Slack:Channels:Types";
        private const string UnreadMessagesCacheKeyPrefix = "Slack:UnreadMessages:";

        private readonly ISlackService _slackService;
        private readonly SlackOptions _options;
        private readonly ILogger<SlackController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _channelsCacheDuration = TimeSpan.FromHours(12); // 頻道清單緩存 12 小時（最長保存時間）
        private readonly TimeSpan _channelsCacheRefreshInterval = TimeSpan.FromHours(1); // 緩存刷新間隔 1 小時（1小時內直接使用緩存）

        public SlackController(ISlackService slackService, IOptions<SlackOptions> options, ILogger<SlackController> logger, IMemoryCache memoryCache)
        {
            _slackService = slackService ?? throw new ArgumentNullException(nameof(slackService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        // OAuth 授權相關方法已移至 SlackQueryController，避免路由衝突
        // 請使用 /slack/authorize 和 /slack/oauth/callback（由 SlackQueryController 處理）

        // POST 操作相關方法已移至 SlackCommandController，避免路由衝突
        // 請使用 SlackCommandController 的對應方法：
        // - SendMessage -> /slack/message (SlackCommandController)

        // GET 查詢相關方法已移至 SlackQueryController，避免路由衝突
        // 請使用 SlackQueryController 的對應方法：
        // - GetMessages -> /slack/messages (SlackQueryController)
        // - GetAllChannels -> /slack/channels/all (SlackQueryController)
        // - GetChannels -> /slack/channels (SlackQueryController)
        // - GetRecentChannels -> /slack/channels/recent (SlackQueryController)
        // - GetUnreadChannels -> /slack/channels/unread (SlackQueryController)
        // - GetUnreadMessages -> /slack/messages/unread (SlackQueryController)

        // 所有 GET 查詢方法已移至 SlackQueryController，避免路由衝突
        // 以下方法已移除：
        // - GetAllChannels (已移至 SlackQueryController)
        // - GetChannels (已移至 SlackQueryController)

        /// <summary>
        /// 取得所有頻道清單（遍歷所有頁面）
        /// </summary>
        private async Task<List<SlackConversationItem>?> FetchAllChannelsAsync(string token, string types, CancellationToken cancellationToken)
        {
            var channels = new List<SlackConversationItem>();
            var request = new SlackConversationsListRequest
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
        private async Task<List<ChannelCacheItem>> ProcessChannelsAsync(List<SlackConversationItem> channels, string token, CancellationToken cancellationToken)
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
        /// 處理緩存的頻道清單，套用搜尋和限制
        /// </summary>
        private async Task<IActionResult> ProcessCachedChannels(List<ChannelCacheItem> cachedChannels, string keyword, int limit, string token, CancellationToken cancellationToken)
        {
            var filteredChannels = cachedChannels
                .Where(item => string.IsNullOrWhiteSpace(keyword)
                    || item.DisplayName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(item.RealName) && item.RealName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                    || item.Id.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(item.UserId) && item.UserId.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                .Select(item => new
                {
                    item.Id,
                    item.RealName,
                    item.DisplayName,
                    item.IsIm,
                    item.IsPrivate,
                    item.IsMpim,
                    item.MemberCount,
                    item.UserId,
                    IsNew = false
                })
                .ToList();

            var results = filteredChannels.Take(limit).ToList();

            // 如果有搜尋關鍵字，也搜尋使用者
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var matchedUserIds = new HashSet<string>(
                    filteredChannels.Where(c => c.IsIm).Select(c => c.UserId),
                    StringComparer.OrdinalIgnoreCase);

                var userMatches = await _slackService.SearchUsersAsync(token, keyword, 8, cancellationToken);

                foreach (var user in userMatches)
                {
                    if (user == null || string.IsNullOrWhiteSpace(user.Id))
                    {
                        continue;
                    }

                    if (matchedUserIds.Contains(user.Id))
                    {
                        continue;
                    }

                    var profile = user.Profile;
                    var displayName = profile?.DisplayName ?? profile?.DisplayNameNormalized;

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = profile?.RealName ?? profile?.RealNameNormalized ?? user.RealName ?? user.Name ?? user.Id;
                    }

                    results.Add(new
                    {
                        Id = string.Empty,
                        user.RealName,
                        DisplayName = displayName,
                        IsIm = true,
                        IsPrivate = false,
                        IsMpim = false,
                        MemberCount = (int?)2,
                        UserId = (string?)user.Id ?? string.Empty,
                        IsNew = true
                    });
                }
            }

            return Ok(results);
        }

        /// <summary>
        /// 背景檢查並更新頻道緩存（API 端點）
        /// </summary>
        [HttpPost("channels/refresh")]
        [IgnoreAntiforgeryToken]
        public IActionResult RefreshChannelsCache([FromBody] RefreshChannelsRequest? request = null)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            // 從 body 或 query string 取得 types 參數
            var types = request?.Types ?? Request.Query["types"].ToString();
            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "private_channel,im" : types;

            // 在背景非同步執行檢查和更新，不等待結果
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckAndRefreshChannelsCacheAsync(token, normalizedTypes, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "背景更新頻道緩存失敗");
                }
            });

            // 立即返回，不等待背景任務完成
            return Ok(new { Message = "背景檢查已啟動" });
        }

        /// <summary>
        /// 檢查並更新頻道緩存（如果時間超過1小時或數量不一致）
        /// </summary>
        private async Task CheckAndRefreshChannelsCacheAsync(string token, string types, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"{ChannelsCacheKey}:{types}";
                var countCacheKey = $"{ChannelsCountCacheKey}:{types}";

                // 檢查緩存是否存在
                if (!_memoryCache.TryGetValue<ChannelsCacheWrapper>(cacheKey, out var cachedWrapper) || cachedWrapper == null || cachedWrapper.Channels == null)
                {
                    // 緩存不存在，直接更新
                    _logger.LogInformation("頻道緩存不存在，開始建立緩存");
                    await RefreshChannelsCacheAsync(token, types, cancellationToken);
                    return;
                }

                // 檢查緩存時間是否超過1小時
                var timeSinceUpdate = DateTime.UtcNow - cachedWrapper.LastUpdated;
                if (timeSinceUpdate >= _channelsCacheRefreshInterval)
                {
                    _logger.LogInformation("緩存時間超過1小時（更新時間：{LastUpdated}，距離現在：{TimeSinceUpdate}），開始更新緩存", 
                        cachedWrapper.LastUpdated, timeSinceUpdate);
                    await RefreshChannelsCacheAsync(token, types, cancellationToken);
                    return;
                }

                // 緩存時間未超過1小時，檢查頻道數量是否一致
                var currentCount = await _slackService.GetUserConversationsCountAsync(token, types, cancellationToken);
                if (!currentCount.HasValue)
                {
                    _logger.LogWarning("無法取得頻道數量，跳過更新");
                    return;
                }

                if (_memoryCache.TryGetValue<int?>(countCacheKey, out var cachedCount))
                {
                    // 如果數量不一致，更新緩存
                    if (cachedCount != currentCount.Value)
                    {
                        _logger.LogInformation("頻道數量不一致（緩存：{CachedCount}，實際：{CurrentCount}），開始更新緩存", cachedCount, currentCount.Value);
                        await RefreshChannelsCacheAsync(token, types, cancellationToken);
                    }
                    else
                    {
                        _logger.LogDebug("頻道數量一致（{Count}），緩存時間正常（{TimeSinceUpdate}），無需更新緩存", 
                            currentCount.Value, timeSinceUpdate);
                    }
                }
                else
                {
                    // 緩存數量不存在，更新緩存
                    _logger.LogInformation("頻道數量緩存不存在，開始更新緩存");
                    await RefreshChannelsCacheAsync(token, types, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查頻道緩存失敗");
            }
        }

        /// <summary>
        /// 重新整理頻道緩存（非同步背景更新）
        /// </summary>
        private async Task RefreshChannelsCacheAsync(string token, string types, CancellationToken cancellationToken)
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

        [HttpPost("events")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Events(CancellationToken cancellationToken)
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // 先解析 JSON 來判斷是否為 url_verification（URL 驗證請求）
            SlackEventEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<SlackEventEnvelope>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 事件失敗：{Body}", body);
                return BadRequest();
            }

            // 處理 URL 驗證請求（Slack 用來驗證 URL 是否可訪問）
            if (envelope?.Type == "url_verification" && !string.IsNullOrWhiteSpace(envelope.Challenge))
            {
                // URL 驗證階段也需要驗證簽章，但如果驗證失敗，仍然回應 challenge（因為這可能是測試請求）
                var timestamp = Request.Headers["X-Slack-Request-Timestamp"].ToString();
                var signature = Request.Headers["X-Slack-Signature"].ToString();

                // 驗證簽章（如果 SigningSecret 未設定，跳過驗證）
                bool signatureValid = false;
                if (!string.IsNullOrWhiteSpace(timestamp) && !string.IsNullOrWhiteSpace(signature))
                {
                    signatureValid = _slackService.ValidateSignature(timestamp, signature, body);
                }

                if (!signatureValid && !string.IsNullOrWhiteSpace(timestamp) && !string.IsNullOrWhiteSpace(signature))
                {
                    _logger.LogWarning("Slack URL 驗證請求的簽章驗證失敗，但仍回應 challenge。Timestamp={Timestamp} Signature={Signature}", timestamp, signature);
                }
                else
                {
                    _logger.LogInformation("Slack URL 驗證成功，回應 challenge。Challenge={Challenge}", envelope.Challenge);
                }

                // 回應 challenge 值（這是 Slack 要求的）
                // 使用 text/plain 並確保正確的編碼
                return Content(envelope.Challenge, "text/plain", Encoding.UTF8);
            }

            // 對於其他事件，必須驗證簽章
            var eventTimestamp = Request.Headers["X-Slack-Request-Timestamp"].ToString();
            var eventSignature = Request.Headers["X-Slack-Signature"].ToString();

            if (!_slackService.ValidateSignature(eventTimestamp, eventSignature, body))
            {
                _logger.LogWarning("Slack 事件簽章驗證失敗。Timestamp={Timestamp} Signature={Signature}", eventTimestamp, eventSignature);
                return Unauthorized();
            }

            if (envelope?.Type == "event_callback" && envelope.Event != null)
            {
                var slackEvent = envelope.Event;

                if (string.Equals(slackEvent.Type, "message", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(slackEvent.Subtype))
                {
                    // 檢查是否為私訊（直接訊息）
                    // Slack 的直接訊息頻道 ID 通常以 'D' 開頭，或 channel_type 為 'im'
                    var isDirectMessage = !string.IsNullOrWhiteSpace(slackEvent.Channel) &&
                        (slackEvent.Channel.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(slackEvent.ChannelType, "im", StringComparison.OrdinalIgnoreCase));

                    if (isDirectMessage && !string.IsNullOrWhiteSpace(slackEvent.Channel))
                    {
                        // 取得接收者的使用者 ID（需要從 session 或配置中取得）
                        // 注意：Slack Events API 不會直接告訴我們接收者是誰
                        // 我們需要透過其他方式判斷，例如從 channel 資訊中取得
                        // 這裡先儲存訊息，後續可以透過 API 查詢頻道資訊來判斷接收者
                        await StoreUnreadMessageAsync(slackEvent, cancellationToken);
                    }

                    _logger.LogInformation("接收到 Slack 訊息：Channel={Channel} User={User} Text={Text} IsDirectMessage={IsDirectMessage}", 
                        slackEvent.Channel, slackEvent.User, slackEvent.Text, isDirectMessage);
                }
            }

            return Ok();
        }

        [HttpPost("channels/open")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OpenChannel([FromBody] SlackOpenChannelRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { Message = "請提供有效的使用者。" });
            }

            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var response = await _slackService.OpenConversationAsync(token, new[] { request.UserId }, cancellationToken);

            if (response?.Ok == true && response.Channel != null && !string.IsNullOrWhiteSpace(response.Channel.Id))
            {
                return Ok(new
                {
                    ChannelId = response.Channel.Id,
                    response.Channel.Name,
                    response.Channel.IsIm,
                    response.Channel.IsPrivate
                });
            }

            var error = response?.Error ?? "未知錯誤";
            _logger.LogWarning("Slack 開啟對話失敗：{Error}", error);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Slack 開啟對話失敗：{error}" });
        }

        // GetRecentChannels 方法已移至 SlackQueryController，避免路由衝突

        // GetUnreadChannels 方法已移至 SlackQueryController，避免路由衝突

        /// <summary>
        /// 儲存未讀訊息到記憶體快取
        /// </summary>
        private async Task StoreUnreadMessageAsync(SlackEventBody slackEvent, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slackEvent.Channel) || string.IsNullOrWhiteSpace(slackEvent.User))
                {
                    return;
                }

                // 取得發送者資訊
                var token = GetEffectiveToken();
                string? senderDisplayName = null;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var userInfo = await _slackService.GetUserInfoAsync(token, slackEvent.User, cancellationToken);
                    if (userInfo?.Ok == true && userInfo.User != null)
                    {
                        var profile = userInfo.User.Profile;
                        senderDisplayName = profile?.DisplayName ?? profile?.DisplayNameNormalized ?? 
                                          profile?.RealName ?? profile?.RealNameNormalized ?? 
                                          userInfo.User.RealName ?? userInfo.User.Name;
                    }
                }

                // 建立未讀訊息物件
                var unreadMessage = new SlackUnreadMessage
                {
                    ChannelId = slackEvent.Channel,
                    SenderUserId = slackEvent.User,
                    SenderDisplayName = senderDisplayName,
                    Text = slackEvent.Text,
                    Timestamp = slackEvent.Timestamp,
                    ReceivedAt = DateTime.UtcNow,
                    IsRead = false
                };

                // 儲存到記憶體快取（以頻道 ID 為鍵，因為我們不知道接收者是誰）
                // 後續可以透過 API 查詢頻道資訊來判斷接收者
                var cacheKey = $"{UnreadMessagesCacheKeyPrefix}{slackEvent.Channel}";
                var existingMessages = _memoryCache.Get<List<SlackUnreadMessage>>(cacheKey) ?? new List<SlackUnreadMessage>();
                
                // 避免重複儲存相同的訊息（根據 timestamp 判斷）
                if (!existingMessages.Any(m => m.Timestamp == slackEvent.Timestamp))
                {
                    existingMessages.Add(unreadMessage);
                    
                    // 只保留最近 100 條未讀訊息
                    if (existingMessages.Count > 100)
                    {
                        existingMessages = existingMessages
                            .OrderByDescending(m => m.ReceivedAt)
                            .Take(100)
                            .ToList();
                    }

                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7), // 保留 7 天
                        SlidingExpiration = null
                    };
                    _memoryCache.Set(cacheKey, existingMessages, cacheOptions);

                    _logger.LogInformation("已儲存未讀訊息：Channel={Channel} Sender={Sender} Text={Text}", 
                        slackEvent.Channel, slackEvent.User, slackEvent.Text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存未讀訊息失敗：Channel={Channel}", slackEvent.Channel);
            }
        }

        // GetUnreadMessages 方法已移至 SlackQueryController，避免路由衝突

        /// <summary>
        /// 標記訊息為已讀
        /// </summary>
        [HttpPost("messages/mark-read")]
        [IgnoreAntiforgeryToken]
        public IActionResult MarkMessageAsRead([FromBody] MarkMessageReadRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ChannelId))
            {
                return BadRequest(new { Message = "請提供頻道 ID。" });
            }

            try
            {
                var cacheKey = $"{UnreadMessagesCacheKeyPrefix}{request.ChannelId}";
                if (_memoryCache.TryGetValue<List<SlackUnreadMessage>>(cacheKey, out var messages) && messages != null)
                {
                    // 如果提供了 timestamp，只標記該訊息為已讀
                    if (!string.IsNullOrWhiteSpace(request.Timestamp))
                    {
                        var message = messages.FirstOrDefault(m => m.Timestamp == request.Timestamp);
                        if (message != null)
                        {
                            message.IsRead = true;
                        }
                    }
                    else
                    {
                        // 否則標記該頻道的所有訊息為已讀
                        foreach (var message in messages)
                        {
                            message.IsRead = true;
                        }
                    }

                    // 更新快取
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                        SlidingExpiration = null
                    };
                    _memoryCache.Set(cacheKey, messages, cacheOptions);

                    return Ok(new { Message = "訊息已標記為已讀。" });
                }

                return Ok(new { Message = "未找到該頻道的訊息。" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "標記訊息為已讀失敗");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"標記訊息為已讀失敗：{ex.Message}" });
            }
        }

        private string? GetEffectiveToken()
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

        public sealed class SlackOpenChannelRequest
        {
            public string? UserId { get; set; }
        }

        /// <summary>
        /// 刷新頻道緩存請求
        /// </summary>
        public sealed class RefreshChannelsRequest
        {
            public string? Types { get; set; }
        }

        /// <summary>
        /// 標記訊息為已讀請求
        /// </summary>
        public sealed class MarkMessageReadRequest
        {
            public string? ChannelId { get; set; }
            public string? Timestamp { get; set; }
        }

        /// <summary>
        /// 頻道緩存項目（用於伺服器端緩存）
        /// </summary>
        private sealed class ChannelCacheItem
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
        private sealed class ChannelsCacheWrapper
        {
            public List<ChannelCacheItem> Channels { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        }
    }
}
