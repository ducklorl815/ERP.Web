using System.Security.Cryptography;
using System.Text;
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
        private readonly ILogger<AzureExamTtsService> _logger;

        public AzureExamTtsService(IOptions<ExamTtsOptions> options, ILogger<AzureExamTtsService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.SubscriptionKey)
            && !string.IsNullOrWhiteSpace(_options.Region);

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

            var fileName = BuildCacheFileName(wordId, speakText, language);
            var filePath = Path.Combine(_options.CacheDirectory, fileName);
            var publicUrl = CombineUrl(_options.PublicUrlPrefix, fileName);

            if (File.Exists(filePath))
                return ExamTtsResult.Ok(publicUrl);

            if (!IsConfigured)
            {
                return ExamTtsResult.Skipped(
                    "尚未設定 Azure Speech（ExamTts:SubscriptionKey）。請註冊後於 appsettings 填入金鑰。");
            }

            try
            {
                var voiceName = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                    ? _options.ChineseVoice
                    : _options.EnglishVoice;

                var speechConfig = SpeechConfig.FromSubscription(_options.SubscriptionKey, _options.Region);
                speechConfig.SpeechSynthesisVoiceName = voiceName;
                speechConfig.SetSpeechSynthesisOutputFormat(
                    SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3);

                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                using var registration = cancellationToken.Register(() => synthesizer.StopSpeakingAsync());

                var synthesis = await synthesizer.SpeakTextAsync(speakText);

                if (synthesis.Reason == ResultReason.Canceled)
                {
                    var details = SpeechSynthesisCancellationDetails.FromResult(synthesis);
                    _logger.LogWarning("Azure TTS 取消：{Error} — {Text}", details.ErrorDetails, speakText);
                    return ExamTtsResult.Fail($"語音合成失敗：{details.ErrorDetails}");
                }

                if (synthesis.AudioData == null || synthesis.AudioData.Length == 0)
                    return ExamTtsResult.Fail("語音合成未回傳音訊資料。");

                await File.WriteAllBytesAsync(filePath, synthesis.AudioData, cancellationToken);
                return ExamTtsResult.Ok(publicUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure TTS 例外，WordID={WordId}", wordId);
                return ExamTtsResult.Fail($"語音合成例外：{ex.Message}");
            }
        }

        /// <summary>快取檔名：WordID + 語言 + 文字 hash，避免同字不同義時覆蓋錯誤。</summary>
        internal static string BuildCacheFileName(Guid wordId, string speakText, string language)
        {
            var hashInput = $"{language}:{speakText.Trim()}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            return $"{wordId:N}_{hash}.mp3";
        }

        private static string CombineUrl(string prefix, string fileName)
        {
            var basePath = (prefix ?? "/exam-audio").TrimEnd('/');
            return $"{basePath}/{fileName}";
        }
    }
}
