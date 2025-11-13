using ERP.Web.Models.Models.Slack;
using ERP.Web.Service.Service.Slack;
using Microsoft.AspNetCore.Mvc;
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
        public SlackCommandController(ISlackService slackService, IOptions<SlackOptions> options, ILogger<SlackCommandController> logger, IMemoryCache memoryCache)
            : base(slackService, options, logger, memoryCache)
        {
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

