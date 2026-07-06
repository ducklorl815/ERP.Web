namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽考卷單題：用於組合完整播放清單</summary>
    public class ExamListeningQuestionItem
    {
        public string SpeakText { get; set; } = string.Empty;
        public string SpeakLanguage { get; set; } = ExamListeningLanguageHelper.LanguageEnglish;
    }
}
