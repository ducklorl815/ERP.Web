namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽考卷單題：用於組合完整播放清單</summary>
    public class ExamListeningQuestionItem
    {
        public Guid WordId { get; set; }

        /// <summary>資料庫 Vocabulary.ExamAudio 已記錄的片段路徑</summary>
        public string? ExamAudio { get; set; }

        public string SpeakText { get; set; } = string.Empty;
        public string SpeakLanguage { get; set; } = ExamListeningLanguageHelper.LanguageEnglish;
    }
}
