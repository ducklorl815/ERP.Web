using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ERP.Web.Options;
using ERP.Web.Services.Slack.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Linq;

namespace ERP.Web.Services.Slack
{
    public class SlackService : ISlackService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly SlackOptions _options;
        private readonly ILogger<SlackService> _logger;
        private readonly Dictionary<string, (SlackUserInfoResponse Response, DateTimeOffset CachedAt)> _userCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _userCacheDuration = TimeSpan.FromMinutes(10);

        public SlackService(HttpClient httpClient, IOptions<SlackOptions> options, ILogger<SlackService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                if (_userCache.TryGetValue(userId, out var cached) && DateTimeOffset.UtcNow - cached.CachedAt < _userCacheDuration)
                {
                    result[userId] = cached.Response;
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
                    _userCache[item.UserId] = (item.Response, DateTimeOffset.UtcNow);
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
            var matches = new List<SlackUser>(limit);
            string? cursor = null;

            do
            {
                var query = new Dictionary<string, string?>
                {
                    ["limit"] = "200",
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
                    break;
                }

                SlackUsersListResponse? listResponse;
                try
                {
                    listResponse = JsonSerializer.Deserialize<SlackUsersListResponse>(responseBody, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "解析 Slack 使用者清單失敗：{Body}", responseBody);
                    break;
                }

                if (listResponse?.Ok != true || listResponse.Members == null)
                {
                    break;
                }

                foreach (var user in listResponse.Members)
                {
                    if (user == null || string.Equals(user.Id, "USLACKBOT", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var displayName = user.Profile?.DisplayNameNormalized ?? user.Profile?.DisplayName;
                    var realName = user.Profile?.RealNameNormalized ?? user.Profile?.RealName ?? user.RealName;
                    var searchable = string.Join(' ', new[] { displayName, realName, user.Name, user.Id })
                        .ToLowerInvariant();

                    if (searchable.Contains(searchTerm.ToLowerInvariant(), StringComparison.Ordinal))
                    {
                        matches.Add(user);
                    }

                    if (matches.Count >= limit)
                    {
                        break;
                    }
                }

                if (matches.Count >= limit)
                {
                    break;
                }

                cursor = listResponse.ResponseMetadata?.NextCursor;

            } while (!string.IsNullOrWhiteSpace(cursor));

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
