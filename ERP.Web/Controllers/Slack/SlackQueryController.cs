using ERP.Web.Models.Models.Slack;
using ERP.Web.Service.Service.Slack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ERP.Web.Controllers.Slack
{
    /// <summary>
    /// Slack 查詢 Controller（GET 方法）- 用於開啟畫面撈取資料
    /// </summary>
    [Route("slack")]
    public class SlackQueryController : SlackControllerBase
    {
        public SlackQueryController(ISlackService slackService, IOptions<SlackOptions> options, ILogger<SlackQueryController> logger, IMemoryCache memoryCache)
            : base(slackService, options, logger, memoryCache)
        {
        }

        /// <summary>
        /// OAuth 授權（導向 Slack 授權頁面）
        /// </summary>
        [HttpGet("authorize")]
        public IActionResult Authorize(string? state = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                return BadRequest("Slack ClientId 未設定，請聯絡系統管理員。");
            }

            var redirectUri = _options.RedirectUrl ?? Url.Action(nameof(OAuthCallback), "SlackQuery", null, Request.Scheme) ?? string.Empty;

            var query = new Dictionary<string, string?>
            {
                ["client_id"] = _options.ClientId,
                ["scope"] = _options.Scopes,
                ["user_scope"] = _options.UserScopes,
                ["redirect_uri"] = redirectUri,
                ["state"] = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state
            };

            var authorizeUrl = QueryHelpers.AddQueryString("https://slack.com/oauth/v2/authorize", query!);

            return Redirect(authorizeUrl);
        }

        /// <summary>
        /// OAuth 回調（處理 Slack 授權回傳）
        /// </summary>
        [HttpGet("oauth/callback")]
        public async Task<IActionResult> OAuthCallback(string? code, string? state, string? error, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Slack OAuth 回傳錯誤：{Error}", error);
                TempData["SlackAuthMessage"] = $"Slack 授權失敗：{error}";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["SlackAuthMessage"] = "Slack 授權失敗：缺少授權碼。";
                return RedirectToAction("Index", "Home");
            }

            var redirectUri = _options.RedirectUrl ?? Url.Action(nameof(OAuthCallback), "SlackQuery", null, Request.Scheme) ?? string.Empty;

            var oauthResponse = await _slackService.ExchangeCodeAsync(code, redirectUri, cancellationToken);

            if (oauthResponse?.Ok == true && !string.IsNullOrWhiteSpace(oauthResponse.AccessToken))
            {
                HttpContext.Session.SetString(SessionBotTokenKey, oauthResponse.AccessToken);
                if (!string.IsNullOrWhiteSpace(oauthResponse.AuthedUser?.AccessToken))
                {
                    HttpContext.Session.SetString(SessionUserTokenKey, oauthResponse.AuthedUser.AccessToken);
                    if (!string.IsNullOrWhiteSpace(oauthResponse.AuthedUser.Id))
                    {
                        HttpContext.Session.SetString(SessionUserIdKey, oauthResponse.AuthedUser.Id);
                    }
                }

                TempData["SlackAuthMessage"] = "Slack 授權成功，可以開始以個人身份傳送訊息。";
                // 設置標記，表示剛完成授權，需要自動顯示聊天框
                TempData["SlackAuthSuccess"] = "true";
            }
            else
            {
                var message = oauthResponse?.Error ?? "未知錯誤";
                TempData["SlackAuthMessage"] = $"Slack 授權失敗：{message}";
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 取得訊息歷史
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(string channel, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return BadRequest(new { Message = "請提供 Slack 頻道 ID。" });
            }

            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var response = await _slackService.GetChannelMessagesAsync(token, channel, Math.Clamp(limit, 1, 100), cancellationToken);

            if (response?.Ok == true)
            {
                return Ok(response.Messages);
            }

            var error = response?.Error ?? "未知錯誤";
            _logger.LogWarning("取得 Slack 歷史訊息失敗：{Error}", error);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Slack 歷史訊息取得失敗：{error}" });
        }

        /// <summary>
        /// 取得完整的頻道清單（包含更新時間），用於存儲到客戶端 sessionStorage
        /// </summary>
        [HttpGet("channels/all")]
        public async Task<IActionResult> GetAllChannels(string types = "private_channel,im,mpim", CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "private_channel,im,mpim" : types;
            var cacheKey = $"{ChannelsCacheKey}:{normalizedTypes}";

            // 嘗試從緩存讀取頻道清單（包含時間戳）
            ChannelsCacheWrapper? cachedWrapper = null;
            if (_memoryCache.TryGetValue<ChannelsCacheWrapper>(cacheKey, out cachedWrapper) && cachedWrapper != null && cachedWrapper.Channels != null)
            {
                var timeSinceUpdate = DateTime.UtcNow - cachedWrapper.LastUpdated;
                
                // 如果緩存時間不到1小時，直接返回緩存
                if (timeSinceUpdate < _channelsCacheRefreshInterval)
                {
                    _logger.LogDebug("使用緩存的頻道清單（更新時間：{LastUpdated}，距離現在：{TimeSinceUpdate}）", 
                        cachedWrapper.LastUpdated, timeSinceUpdate);
                    
                    return Ok(new
                    {
                        Channels = cachedWrapper.Channels.Select(item => new
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
                        }).ToList(),
                        LastUpdated = cachedWrapper.LastUpdated.ToString("O") // ISO 8601 格式
                    });
                }
                
                // 緩存超過1小時，但在背景更新緩存的同時，先返回舊的緩存資料
                _logger.LogInformation("緩存已過期（更新時間：{LastUpdated}，距離現在：{TimeSinceUpdate}），先返回舊緩存，並在背景更新", 
                    cachedWrapper.LastUpdated, timeSinceUpdate);
                
                // 在背景更新緩存（不等待結果）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshChannelsCacheAsync(token, normalizedTypes, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "背景更新頻道緩存失敗");
                    }
                }, cancellationToken);
                
                // 先返回舊的緩存資料
                return Ok(new
                {
                    Channels = cachedWrapper.Channels.Select(item => new
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
                    }).ToList(),
                    LastUpdated = cachedWrapper.LastUpdated.ToString("O") // ISO 8601 格式
                });
            }

            // 緩存不存在，先取得頻道清單並存入緩存
            try
            {
                _logger.LogInformation("緩存不存在，從 Slack API 獲取頻道清單");
                var channels = await FetchAllChannelsAsync(token, normalizedTypes, cancellationToken);
                if (channels == null || channels.Count == 0)
                {
                    _logger.LogWarning("無法取得 Slack 頻道清單（channels 為 null 或空）");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "無法取得 Slack 頻道清單" });
                }

                _logger.LogInformation("成功從 Slack API 取得 {Count} 個頻道，開始處理", channels.Count);

                // 處理並快取頻道清單
                var processedChannels = await ProcessChannelsAsync(channels, token, cancellationToken);
                if (processedChannels == null || processedChannels.Count == 0)
                {
                    _logger.LogWarning("處理後的頻道清單為空");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "處理頻道清單失敗" });
                }

                _logger.LogInformation("成功處理 {Count} 個頻道", processedChannels.Count);

                var now = DateTime.UtcNow;
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

                // 取得頻道數量並存入緩存
                var countCacheKey = $"{ChannelsCountCacheKey}:{normalizedTypes}";
                var channelCount = await _slackService.GetUserConversationsCountAsync(token, normalizedTypes, cancellationToken);
                if (channelCount.HasValue)
                {
                    _memoryCache.Set(countCacheKey, channelCount.Value, cacheOptions);
                    _logger.LogInformation("頻道數量已存入緩存：{Count}", channelCount.Value);
                }

                // 返回處理後的頻道清單（包含更新時間）
                var result = new
                {
                    Channels = processedChannels.Select(item => new
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
                    }).ToList(),
                    LastUpdated = now.ToString("O") // ISO 8601 格式
                };

                _logger.LogInformation("成功返回 {Count} 個頻道給客戶端", result.Channels.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得頻道清單時發生異常");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"取得頻道清單失敗：{ex.Message}" });
            }
        }

        /// <summary>
        /// 取得頻道清單（支援搜尋）
        /// </summary>
        [HttpGet("channels")]
        public async Task<IActionResult> GetChannels(string? query, string types = "private_channel,im", int limit = 20, CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "private_channel,im" : types;
            var keyword = query?.Trim() ?? string.Empty;
            var cacheKey = $"{ChannelsCacheKey}:{normalizedTypes}";
            var countCacheKey = $"{ChannelsCountCacheKey}:{normalizedTypes}";

            // 嘗試從緩存讀取頻道清單（包含時間戳）
            ChannelsCacheWrapper? cachedWrapper = null;
            if (_memoryCache.TryGetValue<ChannelsCacheWrapper>(cacheKey, out cachedWrapper) && cachedWrapper != null && cachedWrapper.Channels != null)
            {
                var timeSinceUpdate = DateTime.UtcNow - cachedWrapper.LastUpdated;
                
                // 如果緩存時間不到1小時，直接使用緩存
                if (timeSinceUpdate < _channelsCacheRefreshInterval)
                {
                    _logger.LogDebug("使用緩存的頻道清單（更新時間：{LastUpdated}，距離現在：{TimeSinceUpdate}）", 
                        cachedWrapper.LastUpdated, timeSinceUpdate);
                    return await ProcessCachedChannels(cachedWrapper.Channels, keyword, limit, token, cancellationToken);
                }
                
                // 緩存超過1小時，但在背景更新緩存的同時，先返回舊的緩存資料
                _logger.LogInformation("緩存已過期（更新時間：{LastUpdated}，距離現在：{TimeSinceUpdate}），先返回舊緩存，並在背景更新", 
                    cachedWrapper.LastUpdated, timeSinceUpdate);
                
                // 在背景更新緩存（不等待結果）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshChannelsCacheAsync(token, normalizedTypes, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "背景更新頻道緩存失敗");
                    }
                }, cancellationToken);
                
                // 先返回舊的緩存資料
                return await ProcessCachedChannels(cachedWrapper.Channels, keyword, limit, token, cancellationToken);
            }

            // 緩存不存在，先取得頻道清單並存入緩存
            _logger.LogInformation("緩存不存在，從 Slack API 獲取頻道清單");
            var channels = await FetchAllChannelsAsync(token, normalizedTypes, cancellationToken);
            if (channels == null || channels.Count == 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "無法取得 Slack 頻道清單" });
            }

            // 處理並快取頻道清單
            var processedChannels = await ProcessChannelsAsync(channels, token, cancellationToken);
            var now = DateTime.UtcNow;
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

            // 取得頻道數量並存入緩存
            var channelCount = await _slackService.GetUserConversationsCountAsync(token, normalizedTypes, cancellationToken);
            if (channelCount.HasValue)
            {
                _memoryCache.Set(countCacheKey, channelCount.Value, cacheOptions);
            }

            // 返回處理後的頻道清單
            return await ProcessCachedChannels(processedChannels, keyword, limit, token, cancellationToken);
        }

        /// <summary>
        /// 取得最近溝通的私人頻道清單（用於預設顯示聊天視窗）
        /// </summary>
        [HttpGet("channels/recent")]
        public async Task<IActionResult> GetRecentChannels(string types = "im,mpim", int limit = 10, CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "im,mpim" : types;
            var normalizedLimit = Math.Clamp(limit, 1, 20); // 最多返回 20 個

            var response = await _slackService.GetRecentConversationsAsync(token, normalizedTypes, normalizedLimit, cancellationToken);

            if (response?.Ok == true && response.Channels != null)
            {
                // 根據 types 參數先過濾頻道（在處理前過濾，保持 Slack API 的原始順序）
                var typesList = normalizedTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .ToHashSet();
                
                // 先過濾原始頻道列表，只保留符合 types 的頻道
                var filteredRawChannels = response.Channels.Where(channel =>
                {
                    if (channel == null) return false;
                    
                    // 如果 types 包含 im，保留 IsIm = true 的頻道
                    if (typesList.Contains("im") && channel.IsIm)
                    {
                        return true;
                    }
                    
                    // 如果 types 包含 mpim，保留 IsMpim = true 的頻道
                    if (typesList.Contains("mpim") && channel.IsMpim)
                    {
                        return true;
                    }
                    
                    // 如果 types 包含 private_channel，保留私密頻道（但不是 im 或 mpim）
                    if (typesList.Contains("private_channel") && channel.IsPrivate && !channel.IsIm && !channel.IsMpim)
                    {
                        return true;
                    }
                    
                    // 如果 types 包含 public_channel，保留公開頻道（但不是私密頻道、im 或 mpim）
                    if (typesList.Contains("public_channel") && channel.IsChannel && !channel.IsPrivate && !channel.IsIm && !channel.IsMpim)
                    {
                        return true;
                    }
                    
                    return false;
                }).ToList();
                
                // 處理過濾後的頻道清單，加入使用者資訊
                var processedChannels = await ProcessChannelsAsync(filteredRawChannels, token, cancellationToken);
                
                // 計算每個頻道的未讀訊息數量並建立字典
                var unreadCountDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
                // 並行取得每個頻道的未讀訊息數量（限制並發數量）
                var semaphore = new SemaphoreSlim(5); // 最多同時 5 個請求
                var unreadTasks = filteredRawChannels
                    .Where(ch => ch != null && !string.IsNullOrWhiteSpace(ch.Id))
                    .Select(async ch =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            // 使用 conversations.history 取得訊息，並根據 last_read 計算未讀數量
                            if (!string.IsNullOrWhiteSpace(ch!.LastRead) && 
                                double.TryParse(ch.LastRead, out var lastReadTs))
                            {
                                // 取得訊息歷史（取得較多訊息以計算未讀數量）
                                var history = await _slackService.GetChannelMessagesAsync(token, ch.Id!, 100, cancellationToken);
                                if (history?.Messages != null)
                                {
                                    // 計算在 last_read 之後的訊息數量
                                    var unreadCount = history.Messages.Count(msg => 
                                    {
                                        if (string.IsNullOrWhiteSpace(msg?.Timestamp)) return false;
                                        if (double.TryParse(msg.Timestamp, out var msgTs))
                                        {
                                            return msgTs > lastReadTs;
                                        }
                                        return false;
                                    });
                                    
                                    if (unreadCount > 0)
                                    {
                                        unreadCountDict[ch.Id!] = unreadCount;
                                    }
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(ch.Latest))
                            {
                                // 如果沒有 last_read，但 latest 存在，可能表示有未讀訊息
                                // 暫時標記為有未讀訊息
                                unreadCountDict[ch.Id!] = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "計算頻道 {ChannelId} 的未讀訊息數量失敗", ch!.Id);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                
                await Task.WhenAll(unreadTasks);
                
                // 根據未讀訊息數量排序（未讀訊息多的排在前面）
                var sortedChannels = processedChannels
                    .OrderByDescending(item =>
                    {
                        // 優先使用未讀訊息數量排序
                        if (item.Id != null && unreadCountDict.TryGetValue(item.Id, out var count))
                        {
                            return count;
                        }
                        return 0;
                    })
                    .Take(normalizedLimit)
                    .ToList();
                
                return Ok(sortedChannels.Select(item =>
                {
                    var unreadCount = item.Id != null && unreadCountDict.TryGetValue(item.Id, out var count) ? count : 0;
                    return new
                    {
                        item.Id,
                        item.RealName,
                        item.DisplayName,
                        item.IsIm,
                        item.IsPrivate,
                        item.IsMpim,
                        item.MemberCount,
                        item.UserId,
                        UnreadCount = unreadCount,
                        UnreadCountDisplay = unreadCount,
                        IsNew = false
                    };
                }).ToList());
            }

            var error = response?.Error ?? "未知錯誤";
            _logger.LogWarning("取得 Slack 最近溝通頻道失敗：{Error}", error);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"取得最近溝通頻道失敗：{error}" });
        }

        /// <summary>
        /// 取得含有未讀訊息的私人頻道
        /// </summary>
        [HttpGet("channels/unread")]
        public async Task<IActionResult> GetUnreadChannels(string types = "private_channel,im,mpim", int limit = 20, CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "private_channel,im,mpim" : types;
            var normalizedLimit = Math.Clamp(limit, 1, 100); // 最多返回 100 個

            // 取得所有對話（使用較大的 limit 以確保取得所有對話）
            var response = await _slackService.GetRecentConversationsAsync(token, normalizedTypes, normalizedLimit, cancellationToken);

            if (response?.Ok == true && response.Channels != null)
            {
                // 根據 types 參數過濾頻道
                var typesList = normalizedTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .ToHashSet();
                
                // 過濾出符合 types 的頻道，並計算未讀訊息數量
                // 注意：unread_count 欄位僅適用於直接訊息（DM），不適用於私人頻道
                // 對於私人頻道，需要根據 last_read 和 latest 時間戳記來計算未讀訊息
                var unreadChannels = new List<(SlackConversationItem Channel, int UnreadCount)>();
                
                foreach (var channel in response.Channels)
                {
                    if (channel == null) continue;
                    
                    // 檢查是否符合 types
                    bool matchesType = false;
                    if (typesList.Contains("im") && channel.IsIm)
                    {
                        matchesType = true;
                    }
                    else if (typesList.Contains("mpim") && channel.IsMpim)
                    {
                        matchesType = true;
                    }
                    else if (typesList.Contains("private_channel") && channel.IsPrivate && !channel.IsIm && !channel.IsMpim)
                    {
                        matchesType = true;
                    }
                    else if (typesList.Contains("public_channel") && channel.IsChannel && !channel.IsPrivate && !channel.IsIm && !channel.IsMpim)
                    {
                        matchesType = true;
                    }
                    
                    if (!matchesType) continue;
                    
                    // 計算未讀訊息數量
                    int unreadCount = 0;
                    
                    // 對於直接訊息（DM），可能會有 unread_count 欄位（但 users.conversations 可能不會返回）
                    // 對於私人頻道，需要根據 last_read 和 latest 來判斷是否有未讀訊息
                    if (!string.IsNullOrWhiteSpace(channel.Latest) && !string.IsNullOrWhiteSpace(channel.LastRead))
                    {
                        if (double.TryParse(channel.Latest, out var latestTs) && 
                            double.TryParse(channel.LastRead, out var lastReadTs))
                        {
                            // 如果 latest > last_read，表示有未讀訊息
                            if (latestTs > lastReadTs)
                            {
                                // 需要取得訊息歷史來計算確切的未讀數量
                                // 暫時標記為有未讀訊息（數量設為 1，表示有未讀）
                                unreadCount = 1;
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(channel.Latest))
                    {
                        // 如果沒有 last_read，但 latest 存在，可能表示有未讀訊息
                        unreadCount = 1;
                    }
                    
                    // 只加入有未讀訊息的頻道
                    if (unreadCount > 0)
                    {
                        unreadChannels.Add((channel, unreadCount));
                    }
                }

                // 處理頻道清單，加入使用者資訊
                var channelList = unreadChannels.Select(uc => uc.Channel).ToList();
                var processedChannels = await ProcessChannelsAsync(channelList, token, cancellationToken);
                
                // 建立未讀訊息數量的字典
                var unreadCountDict = unreadChannels.ToDictionary(uc => uc.Channel.Id!, uc => uc.UnreadCount, StringComparer.OrdinalIgnoreCase);
                
                // 根據未讀訊息數量排序（未讀訊息多的排在前面）
                var sortedChannels = processedChannels
                    .OrderByDescending(item =>
                    {
                        // 優先使用未讀訊息數量排序
                        if (item.Id != null && unreadCountDict.TryGetValue(item.Id, out var count))
                        {
                            return count;
                        }
                        return 0;
                    })
                    .Take(normalizedLimit)
                    .ToList();

                return Ok(sortedChannels.Select(item =>
                {
                    var unreadCount = item.Id != null && unreadCountDict.TryGetValue(item.Id, out var count) ? count : 0;
                    return new
                    {
                        item.Id,
                        item.RealName,
                        item.DisplayName,
                        item.IsIm,
                        item.IsPrivate,
                        item.IsMpim,
                        item.MemberCount,
                        item.UserId,
                        UnreadCount = unreadCount,
                        UnreadCountDisplay = unreadCount, // 使用相同的值
                        IsNew = false
                    };
                }).ToList());
            }

            var error = response?.Error ?? "未知錯誤";
            _logger.LogWarning("取得 Slack 最近溝通頻道失敗：{Error}", error);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"取得最近溝通頻道失敗：{error}" });
        }

        /// <summary>
        /// 取得指定使用者的未讀訊息
        /// </summary>
        [HttpGet("messages/unread")]
        public async Task<IActionResult> GetUnreadMessages(CancellationToken cancellationToken)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            // 取得目前使用者的 ID
            var currentUserId = HttpContext.Session.GetString(SessionUserIdKey);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "無法識別目前使用者，請重新授權。" });
            }

            try
            {
                // 取得使用者的所有直接訊息頻道
                var conversationsResponse = await _slackService.GetUserConversationsAsync(token, new SlackConversationsListRequest
                {
                    Types = "im",
                    ExcludeArchived = true,
                    Limit = 1000
                }, cancellationToken);

                if (conversationsResponse?.Ok != true || conversationsResponse.Channels == null)
                {
                    return Ok(new { Messages = new List<object>() });
                }

                // 收集所有未讀訊息
                var allUnreadMessages = new List<SlackUnreadMessage>();
                foreach (var channel in conversationsResponse.Channels)
                {
                    if (channel?.Id == null) continue;

                    var cacheKey = $"{UnreadMessagesCacheKeyPrefix}{channel.Id}";
                    if (_memoryCache.TryGetValue<List<SlackUnreadMessage>>(cacheKey, out var messages) && messages != null)
                    {
                        // 只取得未讀的訊息
                        var unreadMessages = messages.Where(m => !m.IsRead).ToList();
                        allUnreadMessages.AddRange(unreadMessages);
                    }
                }

                // 按接收時間排序（最新的在前）
                var sortedMessages = allUnreadMessages
                    .OrderByDescending(m => m.ReceivedAt)
                    .Select(m => new
                    {
                        m.ChannelId,
                        m.SenderUserId,
                        m.SenderDisplayName,
                        m.Text,
                        m.Timestamp,
                        m.ReceivedAt,
                        m.IsRead
                    })
                    .ToList();

                return Ok(new { Messages = sortedMessages, Count = sortedMessages.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得未讀訊息失敗");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"取得未讀訊息失敗：{ex.Message}" });
            }
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
    }
}

