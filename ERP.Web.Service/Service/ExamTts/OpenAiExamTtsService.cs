using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ERP.Web.Service.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// OpenAI Audio Speech API：題目文字 → MP3（含本機快取）。
    /// ApiKey 未設定時略過 API，供先上版、後填 Key 使用。
    /// </summary>
    public class OpenAiExamTtsService : IExamTtsService
    {
        private const string SpeechEndpoint = "https://api.openai.com/v1/audio/speech";

        private readonly ExamTtsOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OpenAiExamTtsService> _logger;

        public OpenAiExamTtsService(
            IOptions<ExamTtsOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<OpenAiExamTtsService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.OpenAiApiKey);

        public async Task<ExamTtsResult> GetOrCreateMp3Async(
            Guid wordId,
            string speakText,
            string language,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(speakText))
                return ExamTtsResult.Fail("題目文字為空，無法產生音檔。");

            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
                return ExamTtsResult.Fail("未設定 ExamTts:CacheDirectory。");

            Directory.CreateDirectory(_options.CacheDirectory);

            var fileName = ExamTtsCacheHelper.BuildCacheFileName(wordId, speakText, language);
            var filePath = Path.Combine(_options.CacheDirectory, fileName);
            var publicUrl = ExamTtsCacheHelper.CombineUrl(_options.PublicUrlPrefix, fileName);

            if (File.Exists(filePath))
                return ExamTtsResult.Ok(publicUrl);

            if (!IsConfigured)
            {
                return ExamTtsResult.Skipped(
                    "尚未設定 OpenAI（ExamTts:OpenAiApiKey）。請於 appsettings 或 User Secrets 填入金鑰。");
            }

            try
            {
                var voice = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    ? _options.ChineseVoice
                    : _options.EnglishVoice;

                var payload = new
                {
                    model = _options.OpenAiModel,
                    input = speakText,
                    voice,
                    response_format = "mp3"
                };

                var client = _httpClientFactory.CreateClient(nameof(OpenAiExamTtsService));
                using var request = new HttpRequestMessage(HttpMethod.Post, SpeechEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAiApiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "OpenAI TTS 失敗：{Status} — {Body} — Text={Text}",
                        (int)response.StatusCode,
                        errorBody,
                        speakText);

                    return ExamTtsResult.Fail(
                        $"OpenAI 語音合成失敗（{(int)response.StatusCode}）：{TryParseOpenAiError(errorBody)}");
                }

                var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (audioBytes.Length == 0)
                    return ExamTtsResult.Fail("OpenAI 語音合成未回傳音訊資料。");

                await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
                return ExamTtsResult.Ok(publicUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI TTS 例外，WordID={WordId}", wordId);
                return ExamTtsResult.Fail($"語音合成例外：{ex.Message}");
            }
        }

        /// <summary>從 OpenAI 錯誤 JSON 擷取可讀訊息</summary>
        private static string TryParseOpenAiError(string errorBody)
        {
            if (string.IsNullOrWhiteSpace(errorBody))
                return "未知錯誤";

            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? errorBody;
                }
            }
            catch
            {
                // 非 JSON 時直接回傳原文
            }

            return errorBody.Length > 200 ? errorBody[..200] + "…" : errorBody;
        }
    }
}
