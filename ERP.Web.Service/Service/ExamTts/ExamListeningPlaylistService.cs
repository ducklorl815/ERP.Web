using System.Text;
using ERP.Web.Service.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 英聽考卷：每題產生一支完整 MP3（第 N 題 → 間隔 → 單字 ×2 → 間隔），片段可跨考卷重用。
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
        /// 產生每題英聽音檔：第 N 題 → PauseAfterQuestionLabelSeconds → 單字 → PauseAfterWordSeconds → 單字 → PauseAfterQuestionBlockSeconds。
        /// 存放於 {日期_title}/01.mp3（播放用）＋ 01_jacket.mp3.profile（細節）；共用片段放 Public/。
        /// </summary>
        public async Task<ExamListeningTracksResult> BuildExamQuestionTracksAsync(
            IReadOnlyList<ExamListeningQuestionItem> questions,
            string? examTitle = null,
            DateTime? examDate = null,
            int examAttemptNumber = 1,
            CancellationToken cancellationToken = default)
        {
            if (questions == null || questions.Count == 0)
                return ExamListeningTracksResult.Fail("沒有題目可產生英聽音檔。");

            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
                return ExamListeningTracksResult.Fail("未設定 ExamTts:CacheDirectory。");

            Directory.CreateDirectory(_options.CacheDirectory);
            Directory.CreateDirectory(ExamTtsCacheHelper.GetPublicDirectory(_options.CacheDirectory));

            if (!_examTtsService.IsConfigured)
            {
                return ExamListeningTracksResult.Skipped(
                    "尚未設定 TTS（ExamTts:OpenAiApiKey 或 SubscriptionKey）。");
            }

            var title = string.IsNullOrWhiteSpace(examTitle) ? "exam-listening" : examTitle.Trim();
            var folderDate = examDate ?? DateTime.Today;
            var examFolderName = ExamTtsCacheHelper.BuildExamFolderName(folderDate, title);
            var examFolderPath = Path.Combine(_options.CacheDirectory, examFolderName);
            Directory.CreateDirectory(examFolderPath);

            var tracks = new List<ExamListeningTrackItem>();

            try
            {
                for (var i = 0; i < questions.Count; i++)
                {
                    var questionNumber = i + 1;
                    var question = questions[i];
                    if (string.IsNullOrWhiteSpace(question.SpeakText))
                        return ExamListeningTracksResult.Fail($"第 {questionNumber} 題題目文字為空。");

                    // 播放檔僅題號（01.mp3）；單字細節寫在 01_jacket.mp3.profile
                    var trackFileName = ExamTtsCacheHelper.BuildPerQuestionFileName(questionNumber);
                    var trackProfileFileName = ExamTtsCacheHelper.BuildPerQuestionProfileFileName(
                        questionNumber,
                        question.SpeakText);
                    var trackRelativePath = Path.Combine(examFolderName, trackFileName);
                    var trackPath = Path.Combine(_options.CacheDirectory, trackRelativePath);
                    var trackProfilePath = Path.Combine(examFolderPath, trackProfileFileName);
                    var trackUrl = ExamTtsCacheHelper.CombineUrl(_options.PublicUrlPrefix, trackRelativePath);
                    var trackProfileKey = BuildQuestionTrackProfileKey(questionNumber, question);

                    // 快取命中：直接重用已產生的每題完整音檔
                    if (File.Exists(trackPath)
                        && File.Exists(trackProfilePath)
                        && string.Equals(
                            await File.ReadAllTextAsync(trackProfilePath, cancellationToken),
                            trackProfileKey,
                            StringComparison.Ordinal))
                    {
                        tracks.Add(new ExamListeningTrackItem
                        {
                            QuestionNumber = questionNumber,
                            WordId = question.WordId,
                            AudioUrl = trackUrl,
                            DownloadName = trackFileName,
                            PhysicalPath = trackPath
                        });
                        continue;
                    }

                    // 題號：固定檔名 Public/label_01.mp3…（跨考卷重用）
                    var labelTts = await _examTtsService.GetOrCreateQuestionLabelMp3Async(
                        questionNumber,
                        cancellationToken);

                    if (!labelTts.Success || string.IsNullOrEmpty(labelTts.AudioUrl))
                        return ExamListeningTracksResult.Fail(labelTts.ErrorMessage ?? $"第 {questionNumber} 題題號音檔產生失敗。");

                    var labelPath = labelTts.PhysicalPath
                        ?? ExamTtsCacheHelper.ResolvePhysicalPath(
                            labelTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (labelPath == null || !File.Exists(labelPath))
                        return ExamListeningTracksResult.Fail($"第 {questionNumber} 題題號音檔不存在。");

                    // 單字片段：仍用 seq_ 或 seg_ 快取（Public），避免重複 TTS
                    var segmentFileName = examDate.HasValue
                        ? ExamTtsCacheHelper.BuildExamSegmentFileName(examDate.Value, examAttemptNumber, questionNumber)
                        : null;
                    var wordTts = await _examTtsService.GetOrCreateVocabularySegmentMp3Async(
                        question.WordId,
                        question.ExamAudio,
                        question.SpeakText,
                        question.SpeakLanguage,
                        segmentFileName,
                        cancellationToken);

                    if (!wordTts.Success || string.IsNullOrEmpty(wordTts.AudioUrl))
                        return ExamListeningTracksResult.Fail(wordTts.ErrorMessage ?? $"第 {questionNumber} 題單字音檔產生失敗。");

                    var wordPath = wordTts.PhysicalPath
                        ?? ExamTtsCacheHelper.ResolvePhysicalPath(
                            wordTts.AudioUrl, _options.CacheDirectory, _options.PublicUrlPrefix);
                    if (wordPath == null || !File.Exists(wordPath))
                        return ExamListeningTracksResult.Fail($"第 {questionNumber} 題單字音檔不存在。");

                    if (!string.IsNullOrWhiteSpace(wordPath))
                        question.ExamAudio = wordPath;

                    // 每題完整流程：第 N 題 → 間隔 → 單字 → 間隔 → 單字 → 間隔
                    var segmentPaths = new List<string> { labelPath, wordPath, wordPath };
                    var silenceAfter = new List<double>
                    {
                        _options.PauseAfterQuestionLabelSeconds,
                        _options.PauseAfterWordSeconds,
                        _options.PauseAfterQuestionBlockSeconds
                    };

                    ExamAudioComposer.Compose(segmentPaths, silenceAfter, trackPath);
                    await File.WriteAllTextAsync(trackProfilePath, trackProfileKey, cancellationToken);

                    tracks.Add(new ExamListeningTrackItem
                    {
                        QuestionNumber = questionNumber,
                        WordId = question.WordId,
                        AudioUrl = trackUrl,
                        DownloadName = trackFileName,
                        PhysicalPath = trackPath
                    });
                }

                return ExamListeningTracksResult.Ok(tracks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "產生英聽每題音檔失敗");
                return ExamListeningTracksResult.Fail($"產生英聽音檔失敗：{ex.Message}");
            }
        }

        /// <summary>每題完整音檔的快取比對鍵（pause 或念法變更時重產）</summary>
        internal string BuildQuestionTrackProfileKey(int questionNumber, ExamListeningQuestionItem question)
        {
            var builder = new StringBuilder();
            builder.Append("p=")
                .Append(_options.PauseAfterQuestionLabelSeconds).Append(':')
                .Append(_options.PauseAfterWordSeconds).Append(':')
                .Append(_options.PauseAfterQuestionBlockSeconds)
                .Append("|prov=").Append(_options.Provider)
                .Append("|model=").Append(_options.OpenAiModel)
                .Append("|spd=").Append(_options.Speed.ToString("F2"))
                .Append("|q=").Append(questionNumber)
                .Append('|')
                .Append(question.SpeakLanguage)
                .Append('|')
                .Append(question.SpeakText.Trim());

            return builder.ToString();
        }
    }
}
