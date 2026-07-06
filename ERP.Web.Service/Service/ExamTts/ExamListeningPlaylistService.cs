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
                return ExamTtsResult.Ok(playlistUrl, playlistPath);

            var segmentPaths = new List<string>();
            var silenceAfter = new List<double>();

            try
            {
                for (var i = 0; i < questions.Count; i++)
                {
                    var question = questions[i];
                    if (string.IsNullOrWhiteSpace(question.SpeakText))
                        return ExamTtsResult.Fail($"第 {i + 1} 題題目文字為空。");

                    // 題號：固定檔名 label_01.mp3、label_02.mp3…（HardCode，跨考卷重用）
                    var labelTts = await _examTtsService.GetOrCreateQuestionLabelMp3Async(
                        i + 1,
                        cancellationToken);

                    if (!labelTts.Success || string.IsNullOrEmpty(labelTts.AudioUrl))
                        return ExamTtsResult.Fail(labelTts.ErrorMessage ?? $"第 {i + 1} 題題號音檔產生失敗。");

                    var labelPath = labelTts.PhysicalPath
                        ?? ExamTtsCacheHelper.ResolvePhysicalPath(
                            labelTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (labelPath == null || !File.Exists(labelPath))
                        return ExamTtsResult.Fail($"第 {i + 1} 題題號音檔不存在。");

                    segmentPaths.Add(labelPath);
                    silenceAfter.Add(_options.PauseAfterQuestionLabelSeconds);

                    // 單字片段：優先 Vocabulary.ExamAudio，其次 seg_*.mp3 快取
                    var wordTts = await _examTtsService.GetOrCreateVocabularySegmentMp3Async(
                        question.WordId,
                        question.ExamAudio,
                        question.SpeakText,
                        question.SpeakLanguage,
                        cancellationToken);

                    if (!wordTts.Success || string.IsNullOrEmpty(wordTts.AudioUrl))
                        return ExamTtsResult.Fail(wordTts.ErrorMessage ?? $"第 {i + 1} 題單字音檔產生失敗。");

                    var wordPath = wordTts.PhysicalPath
                        ?? ExamTtsCacheHelper.ResolvePhysicalPath(
                            wordTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (wordPath == null || !File.Exists(wordPath))
                        return ExamTtsResult.Fail($"第 {i + 1} 題單字音檔不存在。");

                    // 同步記憶體中的 ExamAudio（實際寫入 DB 已在 TTS 服務完成）
                    if (!string.IsNullOrWhiteSpace(wordPath))
                        question.ExamAudio = wordPath;

                    segmentPaths.Add(wordPath);
                    silenceAfter.Add(_options.PauseAfterWordSeconds);

                    // 第二遍：直接重用同一檔案，不消耗 token
                    segmentPaths.Add(wordPath);
                    silenceAfter.Add(_options.PauseAfterQuestionBlockSeconds);
                }

                ExamAudioComposer.Compose(segmentPaths, silenceAfter, playlistPath);
                return ExamTtsResult.Ok(playlistUrl, playlistPath);
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
