using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Web.Models.Respository;
using ERP.Web.Service.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// OpenAI Audio Speech API：題目文字 → MP3（含本機快取）。
    /// 支援 speed（語速）與 gpt-4o-mini-tts 的 instructions（口音／語氣）。
    /// </summary>
    public class OpenAiExamTtsService : IExamTtsService
    {
        private const string SpeechEndpoint = "https://api.openai.com/v1/audio/speech";

        private readonly ExamTtsOptions _options;
        private readonly ExamRespo _examRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OpenAiExamTtsService> _logger;

        public OpenAiExamTtsService(
            IOptions<ExamTtsOptions> options,
            ExamRespo examRepo,
            IHttpClientFactory httpClientFactory,
            ILogger<OpenAiExamTtsService> logger)
        {
            _options = options.Value;
            _examRepo = examRepo;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.OpenAiApiKey);

        public Task<ExamTtsResult> GetOrCreateMp3Async(
            Guid wordId,
            string speakText,
            string language,
            CancellationToken cancellationToken = default)
        {
            var profile = ResolveVoiceProfile(language);
            var fileName = ExamTtsCacheHelper.BuildCacheFileName(
                wordId, speakText, language, profile.Key);

            return GetOrCreateCachedMp3Async(fileName, speakText, language, profile, cancellationToken);
        }

        public Task<ExamTtsResult> GetOrCreateSegmentMp3Async(
            string speakText,
            string language,
            CancellationToken cancellationToken = default)
        {
            var profile = ResolveVoiceProfile(language);
            var fileName = ExamTtsCacheHelper.BuildSegmentCacheFileName(
                speakText, language, profile.Key);

            return GetOrCreateCachedMp3Async(fileName, speakText, language, profile, cancellationToken);
        }

        public Task<ExamTtsResult> GetOrCreateQuestionLabelMp3Async(
            int questionNumber,
            CancellationToken cancellationToken = default)
        {
            var fileName = ExamTtsCacheHelper.BuildQuestionLabelFileName(questionNumber);
            var label = ExamListeningLanguageHelper.ToChineseQuestionLabel(questionNumber);
            var profile = ResolveVoiceProfile(ExamListeningLanguageHelper.LanguageChinese);

            return GetOrCreateCachedMp3Async(
                fileName,
                label,
                ExamListeningLanguageHelper.LanguageChinese,
                profile,
                cancellationToken);
        }

        public async Task<ExamTtsResult> GetOrCreateVocabularySegmentMp3Async(
            Guid wordId,
            string? storedExamAudioPath,
            string speakText,
            string language,
            string? segmentFileName = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(speakText))
                return ExamTtsResult.Fail("題目文字為空，無法產生音檔。");

            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
                return ExamTtsResult.Fail("未設定 ExamTts:CacheDirectory。");

            var profile = ResolveVoiceProfile(language);
            var useExamSegmentName = !string.IsNullOrWhiteSpace(segmentFileName);
            var fileName = useExamSegmentName
                ? segmentFileName!
                : ExamTtsCacheHelper.BuildSegmentCacheFileName(speakText, language, profile.Key);
            var filePath = ExamTtsCacheHelper.BuildPhysicalPath(_options.CacheDirectory, fileName);
            var publicUrl = ExamTtsCacheHelper.CombineUrl(_options.PublicUrlPrefix, fileName);
            var segmentProfileKey = ExamTtsCacheHelper.BuildSegmentProfileKey(speakText, language, profile.Key);

            // 0. 記憶體無路徑時，先從 DB 查 Vocabulary.ExamAudio
            var dbExamAudioPath = storedExamAudioPath;
            if (string.IsNullOrWhiteSpace(dbExamAudioPath) && wordId != Guid.Empty)
                dbExamAudioPath = await _examRepo.GetExamAudioAsync(wordId);

            // 1. 優先重用 DB 已記錄路徑（seq_ / seg_ 皆可，須 .profile 比對念法）
            var fromDb = await ExamVocabularyAudioResolver.TryResolveStoredPathAsync(
                dbExamAudioPath, segmentProfileKey, _options.PublicUrlPrefix, cancellationToken);
            if (fromDb != null)
                return fromDb;

            // 2. 嘗試 seg_{hash} 共用快取（相同念法跨考卷，複習考可重用）
            var fromSharedCache = await ExamVocabularyAudioResolver.TryResolveSharedSegmentCacheAsync(
                _options.CacheDirectory,
                _options.PublicUrlPrefix,
                speakText,
                language,
                profile.Key,
                cancellationToken);
            if (fromSharedCache != null)
            {
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, fromSharedCache.PhysicalPath);
                return fromSharedCache;
            }

            // 3. 本次考卷 seq_ 檔名快取（同日期同次數同題號重出卷時重用）
            if (useExamSegmentName
                && await ExamTtsCacheHelper.IsSegmentCacheValidAsync(filePath, segmentProfileKey, cancellationToken))
            {
                var cached = ExamTtsResult.Ok(publicUrl, filePath);
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, cached.PhysicalPath);
                return cached;
            }

            // 4. 非 seq_ 模式：seg_ 檔名直接存在（向後相容）
            if (!useExamSegmentName && File.Exists(filePath))
            {
                var cached = ExamTtsResult.Ok(publicUrl, filePath);
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, cached.PhysicalPath);
                return cached;
            }

            // 5. 呼叫 TTS：新片段一律用 seg_{hash}，利於跨考卷／複習考重用
            var createFileName = ExamTtsCacheHelper.BuildSegmentCacheFileName(speakText, language, profile.Key);
            var created = await GetOrCreateCachedMp3Async(createFileName, speakText, language, profile, cancellationToken);
            if (created.Success && !string.IsNullOrWhiteSpace(created.PhysicalPath))
            {
                await ExamTtsCacheHelper.WriteSegmentProfileAsync(
                    created.PhysicalPath, segmentProfileKey, cancellationToken);
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, created.PhysicalPath);
            }

            return created;
        }

        private async Task<ExamTtsResult> GetOrCreateCachedMp3Async(
            string fileName,
            string speakText,
            string language,
            VoiceProfile profile,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(speakText))
                return ExamTtsResult.Fail("題目文字為空，無法產生音檔。");

            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
                return ExamTtsResult.Fail("未設定 ExamTts:CacheDirectory。");

            Directory.CreateDirectory(_options.CacheDirectory);

            var filePath = Path.Combine(_options.CacheDirectory, fileName);
            var publicUrl = ExamTtsCacheHelper.CombineUrl(_options.PublicUrlPrefix, fileName);

            if (File.Exists(filePath))
                return ExamTtsResult.Ok(publicUrl, filePath);

            if (!IsConfigured)
            {
                return ExamTtsResult.Skipped(
                    "尚未設定 OpenAI（ExamTts:OpenAiApiKey）。請於 appsettings 或 User Secrets 填入金鑰。");
            }

            try
            {
                var payload = BuildPayload(speakText, profile.Voice, profile.Speed, profile.Instructions);
                var json = JsonSerializer.Serialize(payload, JsonOptions);

                var client = _httpClientFactory.CreateClient(nameof(OpenAiExamTtsService));
                using var request = new HttpRequestMessage(HttpMethod.Post, SpeechEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAiApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

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
                return ExamTtsResult.Ok(publicUrl, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI TTS 例外，Text={Text}", speakText);
                return ExamTtsResult.Fail($"語音合成例外：{ex.Message}");
            }
        }

        private VoiceProfile ResolveVoiceProfile(string language)
        {
            var isChinese = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            var speed = ClampSpeed(isChinese
                ? _options.ChineseSpeed ?? _options.Speed
                : _options.EnglishSpeed ?? _options.Speed);
            var voice = isChinese ? _options.ChineseVoice : _options.EnglishVoice;
            var instructions = isChinese
                ? _options.OpenAiChineseInstructions
                : _options.OpenAiEnglishInstructions;
            var key = BuildProfileKey(_options.OpenAiModel, voice, speed, instructions);

            return new VoiceProfile(voice, speed, instructions, key);
        }

        private object BuildPayload(string speakText, string voice, double speed, string? instructions)
        {
            var supportsInstructions = _options.OpenAiModel.StartsWith(
                "gpt-4o-mini-tts",
                StringComparison.OrdinalIgnoreCase);

            if (supportsInstructions && !string.IsNullOrWhiteSpace(instructions))
            {
                return new OpenAiSpeechRequest
                {
                    Model = _options.OpenAiModel,
                    Input = speakText,
                    Voice = voice,
                    ResponseFormat = "mp3",
                    Speed = speed,
                    Instructions = instructions.Trim()
                };
            }

            return new OpenAiSpeechRequest
            {
                Model = _options.OpenAiModel,
                Input = speakText,
                Voice = voice,
                ResponseFormat = "mp3",
                Speed = speed
            };
        }

        internal static string BuildProfileKey(string model, string voice, double speed, string? instructions)
        {
            var instructionPart = string.IsNullOrWhiteSpace(instructions) ? "-" : instructions.Trim();
            return $"{model}|{voice}|{speed:F2}|{instructionPart}";
        }

        internal static double ClampSpeed(double speed) => Math.Clamp(speed, 0.25, 4.0);

        private sealed record VoiceProfile(string Voice, double Speed, string? Instructions, string Key);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private sealed class OpenAiSpeechRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("input")]
            public string Input { get; set; } = string.Empty;

            [JsonPropertyName("voice")]
            public string Voice { get; set; } = string.Empty;

            [JsonPropertyName("response_format")]
            public string ResponseFormat { get; set; } = "mp3";

            [JsonPropertyName("speed")]
            public double Speed { get; set; }

            [JsonPropertyName("instructions")]
            public string? Instructions { get; set; }
        }

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
