using ERP.Web.Models.Models.Slack;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ERP.Web.Service.Service.Slack
{
    public class SlackService : ISlackService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly SlackOptions _options;
        private readonly ILogger<SlackService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _userCacheDuration = TimeSpan.FromHours(24); // 使用者資訊緩存 24 小時

        // 緩存鍵常數
        private const string UserInfoCacheKeyPrefix = "Slack:UserInfo:";
        private const string ChannelsCacheKey = "Slack:Channels";
        private const string ChannelsCountCacheKey = "Slack:ChannelsCount";
        private const string UsersListCacheKey = "Slack:UsersList"; // 使用者清單緩存鍵

        public SlackService(HttpClient httpClient, IOptions<SlackOptions> options, ILogger<SlackService> logger, IMemoryCache memoryCache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://slack.com/api/");
            }
        }

        public async Task<SlackOAuthResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Slack OAuth code 不可為空白。", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                throw new ArgumentException("Slack OAuth redirectUri 不可為空白。", nameof(redirectUri));
            }

            var payload = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId ?? string.Empty,
                ["client_secret"] = _options.ClientSecret ?? string.Empty,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };

            using var content = new FormUrlEncodedContent(payload);
            using var response = await _httpClient.PostAsync("oauth.v2.access", content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack OAuth 交換失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SlackOAuthResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack OAuth 回應失敗：{Body}", responseBody);
                throw;
            }
        }

        public async Task<SlackPostMessageResponse?> PostMessageAsync(string token, SlackPostMessageRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat.postMessage")
            {
                Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack 發送訊息失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SlackPostMessageResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 發送訊息回應失敗：{Body}", responseBody);
                throw;
            }
        }

        public bool ValidateSignature(string requestTimestamp, string signature, string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestTimestamp) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrEmpty(requestBody))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_options.SigningSecret))
            {
                _logger.LogWarning("Slack SigningSecret 未設定，無法驗證簽章。");
                return false;
            }

            var basestring = $"v0:{requestTimestamp}:{requestBody}";
            var secretBytes = Encoding.UTF8.GetBytes(_options.SigningSecret);
            var baseBytes = Encoding.UTF8.GetBytes(basestring);

            using var hasher = new HMACSHA256(secretBytes);
            var hash = hasher.ComputeHash(baseBytes);
            var expected = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

            if (expected.Length != signature.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                difference |= expected[i] ^ signature[i];
            }

            return difference == 0;
        }

        public async Task<SlackConversationsHistoryResponse?> GetChannelMessagesAsync(string token, string channel, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException("Slack channel 不可為空白。", nameof(channel));
            }

            var query = new Dictionary<string, string?>
            {
                ["channel"] = channel,
                ["limit"] = limit.ToString()
            };

            var requestUri = QueryHelpers.AddQueryString("conversations.history", query!);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack 取得歷史訊息失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SlackConversationsHistoryResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 歷史訊息回應失敗：{Body}", responseBody);
                throw;
            }
        }

        public async Task<SlackConversationsListResponse?> GetUserConversationsAsync(string token, SlackConversationsListRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            ArgumentNullException.ThrowIfNull(request);

            var query = new Dictionary<string, string?>
            {
                ["types"] = request.Types,
                ["exclude_archived"] = request.ExcludeArchived ? "true" : "false",
                ["limit"] = request.Limit > 0 ? request.Limit.ToString() : null,
                ["cursor"] = string.IsNullOrWhiteSpace(request.Cursor) ? null : request.Cursor
            };

            var requestUri = QueryHelpers.AddQueryString("users.conversations", query!);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Slack 取得頻道清單失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SlackConversationsListResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 頻道清單回應失敗：{Body}", responseBody);
                throw;
            }
        }

        public async Task<int?> GetUserConversationsCountAsync(string token, string types = "private_channel,im", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            // 計算總數量（需要遍歷所有頁面）
            // 使用較大的 limit 減少 API 呼叫次數
            var query = new Dictionary<string, string?>
            {
                ["types"] = types,
                ["exclude_archived"] = "true",
                ["limit"] = "1000" // 使用較大的 limit 減少 API 呼叫次數
            };

            int totalCount = 0;
            string? cursor = null;

            do
            {
                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    query["cursor"] = cursor;
                }

                var requestUri = QueryHelpers.AddQueryString("users.conversations", query!);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Slack 取得頻道數量失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                    return totalCount > 0 ? totalCount : null;
                }

                try
                {
                    var result = JsonSerializer.Deserialize<SlackConversationsListResponse>(responseBody, JsonOptions);
                    if (result?.Ok != true || result.Channels == null)
                    {
                        break;
                    }

                    totalCount += result.Channels.Count;
                    cursor = result.ResponseMetadata?.NextCursor;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "解析 Slack 頻道數量回應失敗：{Body}", responseBody);
                    break;
                }

            } while (!string.IsNullOrWhiteSpace(cursor));

            return totalCount;
        }

        public async Task<SlackUserInfoResponse?> GetUserInfoAsync(string token, string userId, CancellationToken cancellationToken = default)
        {
            var result = await GetUsersInfoAsync(token, new[] { userId }, cancellationToken);
            return result.TryGetValue(userId, out var value) ? value : null;
        }

        public async Task<IDictionary<string, SlackUserInfoResponse>> GetUsersInfoAsync(string token, IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            ArgumentNullException.ThrowIfNull(userIds);

            var result = new Dictionary<string, SlackUserInfoResponse>(StringComparer.OrdinalIgnoreCase);
            var pending = new List<string>();

            foreach (var userId in userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                // 從伺服器端共享緩存讀取使用者資訊
                var cacheKey = $"{UserInfoCacheKeyPrefix}{userId}";
                if (_memoryCache.TryGetValue<SlackUserInfoResponse>(cacheKey, out var cached))
                {
                    result[userId] = cached;
                }
                else
                {
                    pending.Add(userId);
                }
            }

            if (pending.Count == 0)
            {
                return result;
            }

            var tasks = pending.Select(userId => FetchUserInfoAsync(token, userId, cancellationToken)).ToList();
            var responses = await Task.WhenAll(tasks);

            foreach (var item in responses)
            {
                if (item.Response != null)
                {
                    // 將使用者資訊存入伺服器端共享緩存（所有用戶共享）
                    var cacheKey = $"{UserInfoCacheKeyPrefix}{item.UserId}";
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _userCacheDuration,
                        SlidingExpiration = null // 不使用滑動過期，確保緩存穩定
                    };
                    _memoryCache.Set(cacheKey, item.Response, cacheOptions);
                    result[item.UserId] = item.Response;
                }
            }

            return result;
        }

        private async Task<(string UserId, SlackUserInfoResponse? Response)> FetchUserInfoAsync(string token, string userId, CancellationToken cancellationToken)
        {
            var requestUri = QueryHelpers.AddQueryString("users.info", new Dictionary<string, string?> { ["user"] = userId });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Slack 取得使用者資訊失敗，使用者：{UserId}，狀態碼：{StatusCode}，回應：{Body}", userId, response.StatusCode, responseBody);
                return (userId, null);
            }

            try
            {
                var result = JsonSerializer.Deserialize<SlackUserInfoResponse>(responseBody, JsonOptions);
                return (userId, result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 使用者資訊失敗：{Body}", responseBody);
                return (userId, null);
            }
        }

        public async Task<IReadOnlyCollection<SlackUser>> SearchUsersAsync(string token, string keyword, int limit, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Array.Empty<SlackUser>();
            }

            limit = Math.Clamp(limit, 1, 50);
            var searchTerm = keyword.Trim();

            // 先從緩存讀取使用者清單
            List<SlackUser>? allUsers = null;
            if (_memoryCache.TryGetValue<List<SlackUser>>(UsersListCacheKey, out allUsers) && allUsers != null && allUsers.Count > 0)
            {
                // 緩存存在，直接從緩存搜尋
                return SearchUsersInCache(allUsers, searchTerm, limit);
            }

            // 緩存不存在，從 Slack API 取得所有使用者
            allUsers = await FetchAllUsersAsync(token, cancellationToken);
            if (allUsers == null || allUsers.Count == 0)
            {
                return Array.Empty<SlackUser>();
            }

            // 將使用者清單存入緩存（24 小時）
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _userCacheDuration, // 24 小時
                SlidingExpiration = null
            };
            _memoryCache.Set(UsersListCacheKey, allUsers, cacheOptions);

            // 從緩存搜尋
            return SearchUsersInCache(allUsers, searchTerm, limit);
        }

        /// <summary>
        /// 從 Slack API 取得所有使用者清單
        /// </summary>
        private async Task<List<SlackUser>?> FetchAllUsersAsync(string token, CancellationToken cancellationToken)
        {
            var allUsers = new List<SlackUser>();
            string? cursor = null;

            do
            {
                var query = new Dictionary<string, string?>
                {
                    ["limit"] = "200", // 每次最多取得 200 筆
                    ["cursor"] = cursor
                };

                var requestUri = QueryHelpers.AddQueryString("users.list", query!);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Slack 取得使用者清單失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                    return allUsers.Count > 0 ? allUsers : null;
                }

                SlackUsersListResponse? listResponse;
                try
                {
                    listResponse = JsonSerializer.Deserialize<SlackUsersListResponse>(responseBody, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "解析 Slack 使用者清單失敗：{Body}", responseBody);
                    return allUsers.Count > 0 ? allUsers : null;
                }

                if (listResponse?.Ok != true || listResponse.Members == null)
                {
                    break;
                }

                // 過濾掉 SlackBot 和無效的使用者
                foreach (var user in listResponse.Members)
                {
                    if (user == null || string.Equals(user.Id, "USLACKBOT", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    allUsers.Add(user);
                }

                cursor = listResponse.ResponseMetadata?.NextCursor;

            } while (!string.IsNullOrWhiteSpace(cursor));

            return allUsers;
        }

        /// <summary>
        /// 從緩存的使用者清單中搜尋
        /// </summary>
        private IReadOnlyCollection<SlackUser> SearchUsersInCache(List<SlackUser> allUsers, string searchTerm, int limit)
        {
            var matches = new List<SlackUser>(limit);
            var lowerSearchTerm = searchTerm.ToLowerInvariant();

            foreach (var user in allUsers)
            {
                if (user == null)
                {
                    continue;
                }

                var displayName = user.Profile?.DisplayNameNormalized ?? user.Profile?.DisplayName;
                var realName = user.Profile?.RealNameNormalized ?? user.Profile?.RealName ?? user.RealName;
                var searchable = string.Join(' ', new[] { displayName, realName, user.Name, user.Id })
                    .ToLowerInvariant();

                if (searchable.Contains(lowerSearchTerm, StringComparison.Ordinal))
                {
                    matches.Add(user);

                    if (matches.Count >= limit)
                    {
                        break;
                    }
                }
            }

            return matches;
        }

        public async Task<SlackConversationOpenResponse?> OpenConversationAsync(string token, IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Slack token 不可為空白。", nameof(token));
            }

            ArgumentNullException.ThrowIfNull(userIds);

            var users = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (users.Length == 0)
            {
                throw new ArgumentException("至少需要一個 userId。", nameof(userIds));
            }

            var payload = new
            {
                users = string.Join(',', users)
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "conversations.open")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Slack 開啟對話失敗，狀態碼：{StatusCode}，回應：{Body}", response.StatusCode, responseBody);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SlackConversationOpenResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 Slack 開啟對話回應失敗：{Body}", responseBody);
                throw;
            }
        }
    }
}
