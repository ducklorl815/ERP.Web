using ERP.Web.Hubs;
using ERP.Web.Models.Models.Slack;
using ERP.Web.Service.Service.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ERP.Web.Controllers.Slack
{
    /// <summary>
    /// Slack 指令 Controller（POST 方法）- 用於執行操作指令
    /// </summary>
    [Route("slack")]
    public class SlackCommandController : SlackControllerBase
    {
        private readonly IHubContext<SlackNotificationHub> _hubContext;

        public SlackCommandController(
            ISlackService slackService, 
            IOptions<SlackOptions> options, 
            ILogger<SlackCommandController> logger, 
            IMemoryCache memoryCache,
            IHubContext<SlackNotificationHub> hubContext)
            : base(slackService, options, logger, memoryCache)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        /// <summary>
        /// 發送訊息
        /// </summary>
        [HttpPost("message")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] SlackPostMessageRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "請確認頻道與訊息內容皆已填寫。" });
            }

            var token = GetEffectiveToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Bot Token。" });
            }

            var result = await _slackService.PostMessageAsync(token, request, cancellationToken);

            if (result?.Ok == true)
            {
                return Ok(new { Message = "訊息已成功傳送。", result.Timestamp });
            }

            var errorMessage = result?.Error ?? "未知錯誤";
            _logger.LogWarning("Slack 傳送訊息失敗：{Error}", errorMessage);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Slack 傳送失敗：{errorMessage}" });
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
        /// Slack 事件處理（Webhook）
        /// 支援兩個路由：
        /// - /slack/events（標準路由）
        /// - /SlackCommand/Events（向後兼容路由，用於 Slack Event Subscriptions）
        /// </summary>
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

                // 記錄所有收到的事件（用於調試）
                var textPreview = !string.IsNullOrWhiteSpace(slackEvent.Text) 
                    ? (slackEvent.Text.Length > 50 ? slackEvent.Text.Substring(0, 50) + "..." : slackEvent.Text)
                    : "(無)";
                _logger.LogInformation("收到 Slack 事件：Type={Type} Subtype={Subtype} Channel={Channel} User={User} Text={Text}", 
                    slackEvent.Type, slackEvent.Subtype ?? "(無)", slackEvent.Channel ?? "(無)", slackEvent.User ?? "(無)", textPreview);

                if (string.Equals(slackEvent.Type, "message", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(slackEvent.Subtype))
                {
                    // 檢查是否為私訊（直接訊息）
                    // Slack 的直接訊息頻道 ID 通常以 'D' 開頭，或 channel_type 為 'im'
                    var isDirectMessage = !string.IsNullOrWhiteSpace(slackEvent.Channel) &&
                        (slackEvent.Channel.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(slackEvent.ChannelType, "im", StringComparison.OrdinalIgnoreCase));

                    _logger.LogInformation("訊息類型判斷：IsDirectMessage={IsDirectMessage} Channel={Channel} ChannelType={ChannelType}", 
                        isDirectMessage, slackEvent.Channel, slackEvent.ChannelType ?? "(無)");

                    if (isDirectMessage && !string.IsNullOrWhiteSpace(slackEvent.Channel))
                    {
                        // 儲存未讀訊息
                        var unreadMessage = await StoreUnreadMessageAsync(slackEvent, cancellationToken);
                        
                        // 透過 SignalR 推送即時通知
                        if (unreadMessage != null && !string.IsNullOrWhiteSpace(slackEvent.Channel))
                        {
                            try
                            {
                                // 查詢頻道資訊以確定接收者
                                string? recipientUserId = null;
                                var token = GetEffectiveToken();
                                if (!string.IsNullOrWhiteSpace(token))
                                {
                                    var channelInfo = await _slackService.GetConversationInfoAsync(token, slackEvent.Channel, cancellationToken);
                                    if (channelInfo?.Ok == true && channelInfo.Channel != null)
                                    {
                                        // 對於直接訊息（DM），Channel.User 欄位包含另一個使用者的 ID
                                        // 發送者是 slackEvent.User，接收者是 Channel.User（如果不是發送者的話）
                                        if (channelInfo.Channel.IsIm && !string.IsNullOrWhiteSpace(channelInfo.Channel.User))
                                        {
                                            // 如果 Channel.User 不是發送者，則為接收者
                                            if (!string.Equals(channelInfo.Channel.User, slackEvent.User, StringComparison.OrdinalIgnoreCase))
                                            {
                                                recipientUserId = channelInfo.Channel.User;
                                            }
                                        }
                                        
                                        _logger.LogInformation("查詢頻道資訊：Channel={Channel} IsIm={IsIm} User={User} Sender={Sender} Recipient={Recipient}", 
                                            slackEvent.Channel, channelInfo.Channel.IsIm, channelInfo.Channel.User, slackEvent.User, recipientUserId ?? "未知");
                                    }
                                }
                                
                                // 從暫存中取得使用者資訊（優先根據頻道 ID，其次根據使用者 ID）
                                string? senderDisplayName = unreadMessage.SenderDisplayName;
                                string? senderRealName = null;
                                
                                // 優先從暫存中取得使用者資訊（效能更好）
                                // 在暫存的頻道清單中，每個 IM 頻道都包含使用者的 RealName 和 DisplayName
                                if (string.IsNullOrWhiteSpace(senderDisplayName))
                                {
                                    try
                                    {
                                        // 嘗試從不同類型的暫存中取得頻道資訊
                                        // 注意：暫存鍵的格式是 ChannelsCacheKey:{types}，types 可能包含空格或不同的順序
                                        var cacheKeys = new[] 
                                        { 
                                            $"{ChannelsCacheKey}:private_channel,im,mpim",
                                            $"{ChannelsCacheKey}:im",
                                            $"{ChannelsCacheKey}:private_channel,im",
                                            $"{ChannelsCacheKey}:im,private_channel,mpim", // 不同的順序
                                            $"{ChannelsCacheKey}:im,mpim,private_channel"  // 不同的順序
                                        };
                                        
                                        ChannelCacheItem? foundChannelInfo = null;
                                        string? foundCacheKey = null;
                                        
                                        foreach (var cacheKey in cacheKeys)
                                        {
                                            if (_memoryCache.TryGetValue<ChannelsCacheWrapper>(cacheKey, out var cachedWrapper) 
                                                && cachedWrapper?.Channels != null)
                                            {
                                                _logger.LogDebug("檢查暫存鍵：{CacheKey}，包含 {Count} 個頻道", cacheKey, cachedWrapper.Channels.Count);
                                                
                                                // 優先根據頻道 ID 查找（最準確）
                                                if (!string.IsNullOrWhiteSpace(slackEvent.Channel))
                                                {
                                                    foundChannelInfo = cachedWrapper.Channels
                                                        .FirstOrDefault(c => string.Equals(c.Id, slackEvent.Channel, StringComparison.OrdinalIgnoreCase));
                                                    
                                                    if (foundChannelInfo != null)
                                                    {
                                                        foundCacheKey = cacheKey;
                                                        _logger.LogInformation("從暫存取得頻道資訊（根據頻道 ID）：CacheKey={CacheKey} ChannelId={ChannelId} RealName={RealName} DisplayName={DisplayName} UserId={UserId}", 
                                                            cacheKey, slackEvent.Channel, foundChannelInfo.RealName, foundChannelInfo.DisplayName, foundChannelInfo.UserId);
                                                        break;
                                                    }
                                                }
                                                
                                                // 如果根據頻道 ID 找不到，再根據使用者 ID 查找（IM 頻道的 UserId 就是使用者的 ID）
                                                if (foundChannelInfo == null && !string.IsNullOrWhiteSpace(slackEvent.User))
                                                {
                                                    foundChannelInfo = cachedWrapper.Channels
                                                        .FirstOrDefault(c => c.IsIm 
                                                            && string.Equals(c.UserId, slackEvent.User, StringComparison.OrdinalIgnoreCase));
                                                    
                                                    if (foundChannelInfo != null)
                                                    {
                                                        foundCacheKey = cacheKey;
                                                        _logger.LogInformation("從暫存取得頻道資訊（根據使用者 ID）：CacheKey={CacheKey} UserId={UserId} ChannelId={ChannelId} RealName={RealName} DisplayName={DisplayName}", 
                                                            cacheKey, slackEvent.User, foundChannelInfo.Id, foundChannelInfo.RealName, foundChannelInfo.DisplayName);
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _logger.LogDebug("暫存鍵不存在或為空：{CacheKey}", cacheKey);
                                            }
                                        }
                                        
                                        if (foundChannelInfo != null)
                                        {
                                            // 對於直接訊息（IM），頻道的 RealName 和 DisplayName 就是使用者的名稱
                                            senderRealName = foundChannelInfo.RealName;
                                            senderDisplayName = foundChannelInfo.DisplayName;
                                            
                                            _logger.LogInformation("從暫存取得使用者資訊成功：ChannelId={ChannelId} UserId={UserId} RealName={RealName} DisplayName={DisplayName}", 
                                                foundChannelInfo.Id, slackEvent.User, senderRealName, senderDisplayName);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("暫存中找不到頻道資訊：ChannelId={ChannelId} UserId={UserId}。已嘗試的暫存鍵：{CacheKeys}", 
                                                slackEvent.Channel, slackEvent.User, string.Join(", ", cacheKeys));
                                            
                                            // 列出暫存中所有可用的鍵（用於調試）
                                            _logger.LogDebug("可用的暫存鍵數量：{Count}", cacheKeys.Length);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "從暫存取得使用者資訊失敗：ChannelId={ChannelId} UserId={UserId}", 
                                            slackEvent.Channel, slackEvent.User);
                                    }
                                }
                                
                                // 如果暫存中沒有找到，且 SenderDisplayName 仍為 null，才查詢 Slack API
                                if (string.IsNullOrWhiteSpace(senderDisplayName) && !string.IsNullOrWhiteSpace(slackEvent.User))
                                {
                                    var tokenForUserInfo = GetEffectiveToken();
                                    if (!string.IsNullOrWhiteSpace(tokenForUserInfo))
                                    {
                                        try
                                        {
                                            var userInfo = await _slackService.GetUserInfoAsync(tokenForUserInfo, slackEvent.User, cancellationToken);
                                            if (userInfo?.Ok == true && userInfo.User != null)
                                            {
                                                var profile = userInfo.User.Profile;
                                                // 優先使用 RealName
                                                senderRealName = profile?.RealName ?? profile?.RealNameNormalized ?? 
                                                                userInfo.User.RealName ?? userInfo.User.Name;
                                                // 如果沒有 RealName，使用 DisplayName
                                                senderDisplayName = senderRealName ?? 
                                                                    profile?.DisplayName ?? profile?.DisplayNameNormalized;
                                                
                                                _logger.LogDebug("從 Slack API 取得使用者資訊：UserId={UserId} RealName={RealName} DisplayName={DisplayName}", 
                                                    slackEvent.User, senderRealName, senderDisplayName);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "查詢使用者資訊失敗（推送通知時）：UserId={UserId}", slackEvent.User);
                                        }
                                    }
                                }
                                
                                // 準備通知資料
                                var notificationData = new
                                {
                                    ChannelId = slackEvent.Channel,
                                    SenderUserId = slackEvent.User,
                                    SenderDisplayName = senderDisplayName,
                                    SenderRealName = senderRealName, // 新增 RealName 欄位
                                    Text = slackEvent.Text,
                                    Timestamp = slackEvent.Timestamp,
                                    ReceivedAt = unreadMessage.ReceivedAt,
                                    RecipientUserId = recipientUserId // 包含接收者 ID，前端可以用來過濾
                                };
                                
                                // 如果有確定的接收者，嘗試推送給該使用者的群組；否則推送給所有使用者
                                if (!string.IsNullOrWhiteSpace(recipientUserId))
                                {
                                    // 推送給特定使用者的群組
                                    await _hubContext.Clients.Group($"SlackUser_{recipientUserId}").SendAsync("ReceiveMessage", notificationData);
                                    _logger.LogInformation("已透過 SignalR 推送 Slack 訊息通知給特定使用者：Channel={Channel} Sender={Sender} Recipient={Recipient}", 
                                        slackEvent.Channel, slackEvent.User, recipientUserId);
                                }
                                else
                                {
                                    // 如果無法確定接收者，推送給所有連接的使用者（前端會過濾）
                                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", notificationData);
                                    _logger.LogInformation("已透過 SignalR 推送 Slack 訊息通知給所有使用者（無法確定接收者）：Channel={Channel} Sender={Sender}", 
                                        slackEvent.Channel, slackEvent.User);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "推送 SignalR 通知失敗：Channel={Channel}", slackEvent.Channel);
                            }
                        }
                    }

                    _logger.LogInformation("接收到 Slack 訊息：Channel={Channel} User={User} Text={Text} IsDirectMessage={IsDirectMessage}", 
                        slackEvent.Channel, slackEvent.User, slackEvent.Text, isDirectMessage);
                }
                else
                {
                    // 記錄非直接訊息或不符合條件的事件
                    _logger.LogInformation("跳過處理的訊息事件：Type={Type} Subtype={Subtype} Channel={Channel} IsDirectMessage={IsDirectMessage}", 
                        slackEvent.Type, slackEvent.Subtype ?? "(無)", slackEvent.Channel ?? "(無)", 
                        !string.IsNullOrWhiteSpace(slackEvent.Channel) && 
                        (slackEvent.Channel.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(slackEvent.ChannelType, "im", StringComparison.OrdinalIgnoreCase)));
                }
            }
            else
            {
                // 記錄非 event_callback 類型的事件
                _logger.LogInformation("收到非 event_callback 類型的 Slack 事件：EnvelopeType={EnvelopeType} EventType={EventType}", 
                    envelope?.Type ?? "(無)", envelope?.Event?.Type ?? "(無)");
            }

            return Ok();
        }

        /// <summary>
        /// Slack 事件處理（Webhook）- 向後兼容路由
        /// 此方法用於支援 Slack Event Subscriptions 的 /SlackCommand/Events 路徑
        /// </summary>
        [HttpPost]
        [Route("/SlackCommand/Events")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EventsLegacy(CancellationToken cancellationToken)
        {
            // 直接呼叫主要的事件處理方法
            return await Events(cancellationToken);
        }

        /// <summary>
        /// 測試 SignalR 推送功能（僅用於開發和調試）
        /// </summary>
        [HttpPost("test/notification")]
        [HttpGet("test/notification")] // 同時支援 GET 以便在瀏覽器中直接測試
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TestNotification([FromQuery] string? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // 取得當前使用者的 Slack User ID（如果未提供）
                var targetUserId = userId;
                if (string.IsNullOrWhiteSpace(targetUserId))
                {
                    // 嘗試從 Session 取得
                    targetUserId = HttpContext.Session.GetString("Slack:UserId");
                }

                // 準備測試通知資料
                var testNotification = new
                {
                    ChannelId = "TEST_CHANNEL",
                    SenderUserId = "TEST_SENDER",
                    SenderDisplayName = "測試發送者",
                    Text = $"這是一則測試訊息，發送時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ReceivedAt = DateTime.UtcNow,
                    RecipientUserId = targetUserId
                };

                // 如果有指定的使用者 ID，推送給該使用者的群組；否則推送給所有使用者
                if (!string.IsNullOrWhiteSpace(targetUserId))
                {
                    await _hubContext.Clients.Group($"SlackUser_{targetUserId}").SendAsync("ReceiveMessage", testNotification, cancellationToken);
                    _logger.LogInformation("測試推送：已透過 SignalR 推送測試訊息給使用者群組 SlackUser_{UserId}", targetUserId);
                    return Ok(new { 
                        Message = "測試訊息已推送", 
                        TargetUserId = targetUserId,
                        Notification = testNotification
                    });
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", testNotification, cancellationToken);
                    _logger.LogInformation("測試推送：已透過 SignalR 推送測試訊息給所有連接的使用者");
                    return Ok(new { 
                        Message = "測試訊息已推送給所有使用者（未指定使用者 ID）", 
                        Notification = testNotification
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "測試推送失敗");
                return StatusCode(StatusCodes.Status500InternalServerError, new { 
                    Message = "測試推送失敗", 
                    Error = ex.Message 
                });
            }
        }

        /// <summary>
        /// 開啟頻道（為使用者開啟私訊頻道）
        /// </summary>
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
        /// 儲存未讀訊息到記憶體快取
        /// </summary>
        /// <returns>儲存的未讀訊息物件，如果儲存失敗則返回 null</returns>
        private async Task<SlackUnreadMessage?> StoreUnreadMessageAsync(SlackEventBody slackEvent, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slackEvent.Channel) || string.IsNullOrWhiteSpace(slackEvent.User))
                {
                    return null;
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
                    
                    // 返回儲存的訊息物件
                    return unreadMessage;
                }
                
                // 訊息已存在，返回 null
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存未讀訊息失敗：Channel={Channel}", slackEvent.Channel);
                return null;
            }
        }

        /// <summary>
        /// 開啟頻道請求
        /// </summary>
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
    }
}

