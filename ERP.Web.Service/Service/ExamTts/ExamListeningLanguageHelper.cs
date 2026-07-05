using System.Text.RegularExpressions;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 依題目文字判斷 TTS 語言：含中文 → 中文語音；否則 → 英文語音。
    /// </summary>
    public static class ExamListeningLanguageHelper
    {
        /// <summary>CJK 統一表意文字（簡易判斷中文題目）</summary>
        private static readonly Regex ChineseRegex = new(@"[\u4e00-\u9fff]", RegexOptions.Compiled);

        public const string LanguageChinese = "zh-TW";
        public const string LanguageEnglish = "en-US";

        /// <summary>
        /// 題目含中文 → zh-TW；否則 en-US。
        /// </summary>
        public static string ResolveLanguage(string? questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
                return LanguageEnglish;

            return ChineseRegex.IsMatch(questionText)
                ? LanguageChinese
                : LanguageEnglish;
        }

        /// <summary>英聽時念題目本身（Question），不念 Answer。</summary>
        public static string ResolveSpeakText(string? questionText)
        {
            return questionText?.Trim() ?? string.Empty;
        }
    }
}
