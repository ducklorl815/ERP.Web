using System.Text;
using ERP.Web.Service.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 英聽考卷：將「第 N 題 → 單字 ×2」組成一支完整 MP3，片段可跨考卷重用。
    /// </summary>
    public class ExamListeningPlaylistService
    {
        private readonly IExamTtsService _examTtsService;
        private readonly ExamTtsOptions _options;
        private readonly ILogger<ExamListeningPlaylistService> _logger;

        public ExamListeningPlaylistService(
            IExamTtsService examTtsService,
            IOptions<ExamTtsOptions> options,
            ILogger<ExamListeningPlaylistService> logger)
        {
            _examTtsService = examTtsService;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 產生整份考卷英聽音檔：第 N 題 → 3 秒 → 單字 → 3 秒 → 單字 → 10 秒 → 下一題。
        /// </summary>
        public async Task<ExamTtsResult> BuildExamPlaylistAsync(
            IReadOnlyList<ExamListeningQuestionItem> questions,
            CancellationToken cancellationToken = default)
        {
            if (questions == null || questions.Count == 0)
                return ExamTtsResult.Fail("沒有題目可產生英聽音檔。");

            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
                return ExamTtsResult.Fail("未設定 ExamTts:CacheDirectory。");

            Directory.CreateDirectory(_options.CacheDirectory);

            if (!_examTtsService.IsConfigured)
            {
                return ExamTtsResult.Skipped(
                    "尚未設定 TTS（ExamTts:OpenAiApiKey 或 SubscriptionKey）。");
            }

            var playlistProfileKey = BuildPlaylistProfileKey(questions);
            var playlistFileName = ExamTtsCacheHelper.BuildPlaylistCacheFileName(playlistProfileKey);
            var playlistPath = Path.Combine(_options.CacheDirectory, playlistFileName);
            var playlistUrl = ExamTtsCacheHelper.CombineUrl(_options.PublicUrlPrefix, playlistFileName);

            if (File.Exists(playlistPath))
                return ExamTtsResult.Ok(playlistUrl);

            var segmentPaths = new List<string>();
            var silenceAfter = new List<double>();

            try
            {
                for (var i = 0; i < questions.Count; i++)
                {
                    var question = questions[i];
                    if (string.IsNullOrWhiteSpace(question.SpeakText))
                        return ExamTtsResult.Fail($"第 {i + 1} 題題目文字為空。");

                    // 題號（中文，可跨考卷重用：第一題、第二題…）
                    var label = ExamListeningLanguageHelper.ToChineseQuestionLabel(i + 1);
                    var labelTts = await _examTtsService.GetOrCreateSegmentMp3Async(
                        label,
                        ExamListeningLanguageHelper.LanguageChinese,
                        cancellationToken);

                    if (!labelTts.Success || string.IsNullOrEmpty(labelTts.AudioUrl))
                        return ExamTtsResult.Fail(labelTts.ErrorMessage ?? $"第 {i + 1} 題題號音檔產生失敗。");

                    var labelPath = ExamTtsCacheHelper.ResolvePhysicalPath(
                        labelTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (labelPath == null || !File.Exists(labelPath))
                        return ExamTtsResult.Fail($"第 {i + 1} 題題號音檔不存在。");

                    segmentPaths.Add(labelPath);
                    silenceAfter.Add(_options.PauseAfterQuestionLabelSeconds);

                    // 題目單字（可重用：相同 brilliant / 忙碌的 只呼叫 API 一次）
                    var wordTts = await _examTtsService.GetOrCreateSegmentMp3Async(
                        question.SpeakText,
                        question.SpeakLanguage,
                        cancellationToken);

                    if (!wordTts.Success || string.IsNullOrEmpty(wordTts.AudioUrl))
                        return ExamTtsResult.Fail(wordTts.ErrorMessage ?? $"第 {i + 1} 題單字音檔產生失敗。");

                    var wordPath = ExamTtsCacheHelper.ResolvePhysicalPath(
                        wordTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (wordPath == null || !File.Exists(wordPath))
                        return ExamTtsResult.Fail($"第 {i + 1} 題單字音檔不存在。");

                    segmentPaths.Add(wordPath);
                    silenceAfter.Add(_options.PauseAfterWordSeconds);

                    // 第二遍：直接重用同一檔案，不消耗 token
                    segmentPaths.Add(wordPath);
                    silenceAfter.Add(_options.PauseAfterQuestionBlockSeconds);
                }

                ExamAudioComposer.Compose(segmentPaths, silenceAfter, playlistPath);
                return ExamTtsResult.Ok(playlistUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合併英聽播放清單失敗");
                return ExamTtsResult.Fail($"合併英聽音檔失敗：{ex.Message}");
            }
        }

        internal string BuildPlaylistProfileKey(IReadOnlyList<ExamListeningQuestionItem> questions)
        {
            var builder = new StringBuilder();
            builder.Append("p=")
                .Append(_options.PauseAfterQuestionLabelSeconds).Append(':')
                .Append(_options.PauseAfterWordSeconds).Append(':')
                .Append(_options.PauseAfterQuestionBlockSeconds)
                .Append("|prov=").Append(_options.Provider)
                .Append("|model=").Append(_options.OpenAiModel)
                .Append("|spd=").Append(_options.Speed.ToString("F2"))
                .Append('|');

            for (var i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                builder.Append(i + 1)
                    .Append('|')
                    .Append(q.SpeakLanguage)
                    .Append('|')
                    .Append(q.SpeakText.Trim())
                    .Append(';');
            }

            return builder.ToString();
        }
    }
}
