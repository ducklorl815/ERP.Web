using ERP.Web.Models.Respository;
using ERP.Web.Service.Options;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// Azure Cognitive Services Speech：題目文字 → MP3（含本機快取）。
    /// SubscriptionKey 未設定時略過 API，供先上版、後填 Key 使用。
    /// </summary>
    public class AzureExamTtsService : IExamTtsService
    {
        private readonly ExamTtsOptions _options;
        private readonly ExamRespo _examRepo;
        private readonly ILogger<AzureExamTtsService> _logger;

        public AzureExamTtsService(
            IOptions<ExamTtsOptions> options,
            ExamRespo examRepo,
            ILogger<AzureExamTtsService> logger)
        {
            _options = options.Value;
            _examRepo = examRepo;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.SubscriptionKey)
            && !string.IsNullOrWhiteSpace(_options.Region);

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

            var dbExamAudioPath = storedExamAudioPath;
            if (string.IsNullOrWhiteSpace(dbExamAudioPath) && wordId != Guid.Empty)
                dbExamAudioPath = await _examRepo.GetExamAudioAsync(wordId);

            var fromDb = await ExamVocabularyAudioResolver.TryResolveStoredPathAsync(
                dbExamAudioPath, segmentProfileKey, _options.PublicUrlPrefix, cancellationToken);
            if (fromDb != null)
                return fromDb;

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

            if (useExamSegmentName
                && await ExamTtsCacheHelper.IsSegmentCacheValidAsync(filePath, segmentProfileKey, cancellationToken))
            {
                var cached = ExamTtsResult.Ok(publicUrl, filePath);
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, cached.PhysicalPath);
                return cached;
            }

            if (!useExamSegmentName && File.Exists(filePath))
            {
                var cached = ExamTtsResult.Ok(publicUrl, filePath);
                await ExamVocabularyAudioPersistence.TrySaveAsync(
                    _examRepo, _logger, wordId, dbExamAudioPath, cached.PhysicalPath);
                return cached;
            }

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
                    "尚未設定 Azure Speech（ExamTts:SubscriptionKey）。請註冊後於 appsettings 填入金鑰。");
            }

            try
            {
                var speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
                speechConfig.SpeechSynthesisVoiceName = profile.VoiceName;
                speechConfig.SetSpeechSynthesisOutputFormat(
                    SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3);

                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                using var registration = cancellationToken.Register(() => synthesizer.StopSpeakingAsync());

                var ratePercent = (int)Math.Round(profile.Speed * 100);
                var xmlLang = profile.IsChinese ? "zh-TW" : "en-US";
                var escapedText = System.Security.SecurityElement.Escape(speakText) ?? speakText;
                var ssml =
                    $"<speak version='1.0' xml:lang='{xmlLang}'>" +
                    $"<voice name='{profile.VoiceName}'>" +
                    $"<prosody rate='{ratePercent}%'>{escapedText}</prosody>" +
                    "</voice></speak>";

                var synthesis = await synthesizer.SpeakSsmlAsync(ssml);

                if (synthesis.Reason == ResultReason.Canceled)
                {
                    var details = SpeechSynthesisCancellationDetails.FromResult(synthesis);
                    _logger.LogWarning("Azure TTS 取消：{Error} — {Text}", details.ErrorDetails, speakText);
                    return ExamTtsResult.Fail($"語音合成失敗：{details.ErrorDetails}");
                }

                if (synthesis.AudioData == null || synthesis.AudioData.Length == 0)
                    return ExamTtsResult.Fail("語音合成未回傳音訊資料。");

                await File.WriteAllBytesAsync(filePath, synthesis.AudioData, cancellationToken);
                return ExamTtsResult.Ok(publicUrl, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure TTS 例外，Text={Text}", speakText);
                return ExamTtsResult.Fail($"語音合成例外：{ex.Message}");
            }
        }

        private VoiceProfile ResolveVoiceProfile(string language)
        {
            var isChinese = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            var speed = OpenAiExamTtsService.ClampSpeed(isChinese
                ? _options.ChineseSpeed ?? _options.Speed
                : _options.EnglishSpeed ?? _options.Speed);
            var voiceName = isChinese ? _options.ChineseVoice : _options.EnglishVoice;
            var key = $"azure|{voiceName}|{speed:F2}";

            return new VoiceProfile(voiceName, speed, isChinese, key);
        }

        private sealed record VoiceProfile(string VoiceName, double Speed, bool IsChinese, string Key);
    }
}
