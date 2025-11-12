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

        [HttpGet("authorize")]
        public IActionResult Authorize(string? state = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                return BadRequest("Slack ClientId 未設定，請聯絡系統管理員。");
            }

            var redirectUri = _options.RedirectUrl ?? Url.Action(nameof(OAuthCallback), "Slack", null, Request.Scheme) ?? string.Empty;

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

            var redirectUri = _options.RedirectUrl ?? Url.Action(nameof(OAuthCallback), "Slack", null, Request.Scheme) ?? string.Empty;

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
        public async Task<IActionResult> GetAllChannels(string types = "private_channel,im", CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var normalizedTypes = string.IsNullOrWhiteSpace(types) ? "private_channel,im" : types;
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
                            item.Name,
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
                        item.Name,
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
                        item.Name,
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

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = profile?.DisplayNameNormalized;
                        }

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = profile?.RealName ?? profile?.RealNameNormalized;
                        }

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = userInfo.User?.RealName ?? userInfo.User?.Name;
                        }

                        userNameCache[userId] = string.IsNullOrWhiteSpace(displayName) ? userId : displayName!;
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

                    return new ChannelCacheItem
                    {
                        Id = id,
                        Name = channel.Name,
                        DisplayName = displayName,
                        IsIm = channel.IsIm,
                        IsPrivate = channel.IsPrivate,
                        IsMpim = channel.IsMpim,
                        MemberCount = channel.MemberCount,
                        UserId = userIdValue,
                        IsNew = false
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
                    || (!string.IsNullOrWhiteSpace(item.Name) && item.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                    || item.Id.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(item.UserId) && item.UserId.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                .Select(item => new
                {
                    item.Id,
                    item.Name,
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
                        user.Name,
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

            var timestamp = Request.Headers["X-Slack-Request-Timestamp"].ToString();
            var signature = Request.Headers["X-Slack-Signature"].ToString();

            if (!_slackService.ValidateSignature(timestamp, signature, body))
            {
                _logger.LogWarning("Slack 簽章驗證失敗。Timestamp={Timestamp} Signature={Signature}", timestamp, signature);
                return Unauthorized();
            }

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

            if (envelope?.Type == "url_verification" && !string.IsNullOrWhiteSpace(envelope.Challenge))
            {
                return Content(envelope.Challenge, "text/plain", Encoding.UTF8);
            }

            if (envelope?.Type == "event_callback" && envelope.Event != null)
            {
                var slackEvent = envelope.Event;

                if (string.Equals(slackEvent.Type, "message", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(slackEvent.Subtype))
                {
                    // TODO: 將訊息內容儲存到資料庫或快取，供前端聊天視窗載入。
                    _logger.LogInformation("接收到 Slack 訊息：Channel={Channel} User={User} Text={Text}", slackEvent.Channel, slackEvent.User, slackEvent.Text);
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
        /// 頻道緩存項目（用於伺服器端緩存）
        /// </summary>
        private sealed class ChannelCacheItem
        {
            public string Id { get; set; } = string.Empty;
            public string? Name { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public bool IsIm { get; set; }
            public bool IsPrivate { get; set; }
            public bool IsMpim { get; set; }
            public int? MemberCount { get; set; }
            public string UserId { get; set; } = string.Empty;
            public bool IsNew { get; set; }
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
