using ERP.Web.Options;
using ERP.Web.Services.Slack;
using ERP.Web.Services.Slack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ERP.Web.Controllers
{
    [Route("slack")]
    public class SlackController : Controller
    {
        private const string SessionBotTokenKey = "Slack:BotToken";
        private const string SessionUserTokenKey = "Slack:UserToken";
        private const string SessionUserIdKey = "Slack:UserId";

        private readonly ISlackService _slackService;
        private readonly SlackOptions _options;
        private readonly ILogger<SlackController> _logger;

        public SlackController(ISlackService slackService, IOptions<SlackOptions> options, ILogger<SlackController> logger)
        {
            _slackService = slackService ?? throw new ArgumentNullException(nameof(slackService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                return Ok(new { Message = "訊息已成功傳送。", Timestamp = result.Timestamp });
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

        [HttpGet("channels")]
        public async Task<IActionResult> GetChannels(string? query, string types = "private_channel,im", int limit = 20, CancellationToken cancellationToken = default)
        {
            var token = GetEffectiveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new { Message = "尚未完成 Slack 授權或未設定 Token。" });
            }

            var request = new SlackConversationsListRequest
            {
                Types = string.IsNullOrWhiteSpace(types) ? "private_channel,im" : types,
                Limit = Math.Clamp(limit, 1, 200)
            };

            var channels = new List<SlackConversationItem>();
            SlackConversationsListResponse? response;
            var keyword = query?.Trim() ?? string.Empty;

            var maxFetch = string.IsNullOrWhiteSpace(keyword) ? request.Limit : 500;

            do
            {
                response = await _slackService.GetUserConversationsAsync(token, request, cancellationToken);

                if (response?.Ok != true)
                {
                    var error = response?.Error ?? "未知錯誤";
                    _logger.LogWarning("取得 Slack 頻道清單失敗：{Error}", error);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = $"Slack 頻道清單取得失敗：{error}" });
                }

                if (response.Channels?.Count > 0)
                {
                    channels.AddRange(response.Channels);
                }

                request.Cursor = response.ResponseMetadata?.NextCursor;

            } while (!string.IsNullOrWhiteSpace(request.Cursor) && channels.Count < maxFetch);

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
                        : (!string.IsNullOrWhiteSpace(channel.User) && userNameCache.TryGetValue(channel.User, out var userDisplay)
                            ? userDisplay
                            : (!string.IsNullOrWhiteSpace(channel.User) ? channel.User : id));
                    var userIdValue = channel.User ?? string.Empty;

                    return new
                    {
                        Id = id,
                        Name = channel.Name,
                        DisplayName = displayName,
                        channel.IsIm,
                        channel.IsPrivate,
                        channel.IsMpim,
                        channel.MemberCount,
                        UserId = userIdValue,
                        IsNew = false
                    };
                })
                .Where(item => string.IsNullOrWhiteSpace(keyword)
                    || item.DisplayName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(item.Name) && item.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                    || item.Id.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(item => item.IsIm ? 0 : 1)
                .ThenBy(item => item.DisplayName)
                .ToList();

            var results = conversationItems.Take(limit).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var matchedUserIds = new HashSet<string>(imChannels, StringComparer.OrdinalIgnoreCase);
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
                        Name = user.Name,
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
    }
}
