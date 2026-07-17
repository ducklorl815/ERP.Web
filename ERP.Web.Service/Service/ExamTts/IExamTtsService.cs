namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽考卷：文字轉語音（MP3）服務</summary>
    public interface IExamTtsService
    {
        /// <summary>Azure Key 是否已設定（未設定時不呼叫 API）</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// 取得或建立 MP3；快取檔名含 WordID 與文字 hash，文字變更時會重產。
        /// </summary>
        Task<ExamTtsResult> GetOrCreateMp3Async(
            Guid wordId,
            string speakText,
            string language,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得或建立可重複使用的 TTS 片段（依文字 hash，跨考卷共用以節省 API）。
        /// </summary>
        Task<ExamTtsResult> GetOrCreateSegmentMp3Async(
            string speakText,
            string language,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 題號播音（第一題、第二題…）固定檔名 label_01.mp3，可跨考卷重用。
        /// </summary>
        Task<ExamTtsResult> GetOrCreateQuestionLabelMp3Async(
            int questionNumber,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 單字聽力片段：優先查 Vocabulary.ExamAudio 與 seg_{hash} 共用快取；
        /// 僅在無快取時呼叫 TTS，並寫回 Vocabulary.ExamAudio。
        /// </summary>
        Task<ExamTtsResult> GetOrCreateVocabularySegmentMp3Async(
            Guid wordId,
            string? storedExamAudioPath,
            string speakText,
            string language,
            string? segmentFileName = null,
            CancellationToken cancellationToken = default);
    }
}
