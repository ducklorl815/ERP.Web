using System.Text.RegularExpressions;
using ERP.Web.Models.Models;

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

        public const string DirectionEnglish = "English";
        public const string DirectionChinese = "Chinese";

        /// <summary>
        /// 聽力出題：英文聽力念 Answer、中文聽力念 Question，並設定答案卷應填欄位。
        /// </summary>
        public static void ApplyListeningProfile(Vocabulary word, string direction)
        {
            if (string.Equals(direction, DirectionEnglish, StringComparison.OrdinalIgnoreCase))
            {
                word.ListeningDirection = DirectionEnglish;
                word.SpeakText = (word.Answer ?? string.Empty).Trim();
                word.SpeakLanguage = LanguageEnglish;
                word.ListeningExpectedAnswer = word.Question;
                return;
            }

            word.ListeningDirection = DirectionChinese;
            word.SpeakText = (word.Question ?? string.Empty).Trim();
            word.SpeakLanguage = LanguageChinese;
            word.ListeningExpectedAnswer = word.Answer;
        }

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

        private static readonly string[] ChineseDigits =
            { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

        /// <summary>題號播音文字，例如：第一題、第二題（固定以中文念出）</summary>
        public static string ToChineseQuestionLabel(int questionNumber)
        {
            if (questionNumber <= 0)
                return $"第{questionNumber}題";

            return $"第{ToChineseNumber(questionNumber)}題";
        }

        /// <summary>將 1～99 轉為中文數字</summary>
        public static string ToChineseNumber(int number)
        {
            if (number < 0)
                return number.ToString();

            if (number < 10)
                return ChineseDigits[number];

            if (number == 10)
                return "十";

            if (number < 20)
                return "十" + ChineseDigits[number % 10];

            if (number < 100)
            {
                var tens = number / 10;
                var ones = number % 10;
                if (ones == 0)
                    return ChineseDigits[tens] + "十";

                return ChineseDigits[tens] + "十" + ChineseDigits[ones];
            }

            return number.ToString();
        }
    }
}
