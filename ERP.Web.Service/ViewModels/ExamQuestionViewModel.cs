using ERP.Web.Models.Models;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Mvc.Rendering; // ASP.NET Core 的 SelectListItem

namespace ERP.Web.Service.ViewModels
{
    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
    }
    public class ExamSearchListViewModel_param : PageViewModel
    {
        public List<string> ClassNameList { get; set; }
        public string TestType { get; set; }
        public string CorrectType { get; set; }
        public string TestDate { get; set; }
        public string KidID { get; set; }
        public string WordID { get; set; }
        public bool Correct { get; set; }
        public bool Focus { get; set; }// 優先試題
        public int TestNumber { get; set; }

        /// <summary>英聽題數：聽英文（Answer）→ 寫中文（Question）</summary>
        public int EnglishListeningCount { get; set; }

        /// <summary>中聽題數：聽中文（Question）→ 寫英文（Answer）</summary>
        public int ChineseListeningCount { get; set; }

        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
        /// <summary>關鍵字：課程、問題、答案一併搜尋</summary>
        public string SearchText { get; set; }

        /// <summary>Written（筆試，預設）| Listening（英聽）</summary>
        public string ExamMode { get; set; } = "Written";
    }
    public class ExamSearchListViewModel_result : ExamSearchListViewModel_param
    {
        public string TestDate { get; set; }
        public List<ExamMainModel> ExamDataList { get; set; }
        public List<string> ClassNameList { get; set; }
        public List<SelectListItem> KidList { get; set; }
        public List<SelectListItem> TestDateList { get; set; }
        /// <summary>各課程（辭庫）答對／答錯／複習次數統計</summary>
        public List<ClassNameStatModel> ClassNameStats { get; set; }
        /// <summary>課程下拉（Value=ClassName，Text 含統計）</summary>
        public List<SelectListItem> ClassNameSelectList { get; set; }
        public List<SelectListItem> TestTypeList
        {
            get
            {
                return new List<SelectListItem>
            {
                new SelectListItem { Text = "英文", Value = "English" },
                new SelectListItem { Text = "數學", Value = "Math" }
            };
            }
        }

        public List<SelectListItem> CorrectList
        {
            get
            {
                return new List<SelectListItem>
            {
                new SelectListItem { Text = "新試題", Value = "0" },
                new SelectListItem { Text = "複習錯誤試題", Value = "1" }
            };
            }
        }
    }

    public class ExamDataViewModel_result
    {
        public int score { get; set; }
        public ScoreTable scoreTable { get; set; }
        public List<Vocabulary> VocabularyList { get; set; }
        public string Title { get; set; }
        /// <summary>考卷日期（來自 KidTestIndex.TestDate 或當日出卷）</summary>
        public DateTime ExamDate { get; set; } = DateTime.Today;
        /// <summary>該學生在此 Lession 的第幾次考試（依 KidTestIndex 累計）</summary>
        public int ExamAttemptNumber { get; set; } = 1;
        /// <summary>試卷類型顯示：英文、數學</summary>
        public string ExamTypeLabel { get; set; } = "英文";
        /// <summary>考卷標題後綴，例如：英文試卷_20260523_2</summary>
        public string ExamPaperSuffix =>
            $"{ExamTypeLabel}試卷_{ExamDate:yyyyMMdd}_{ExamAttemptNumber}";

        /// <summary>與考卷 exam-title 一致的名稱，例如：複習考_20260707 英聽試卷_20260707_7</summary>
        public string ExamListeningDisplayName => $"{Title} {ExamPaperSuffix}".Trim();

        /// <summary>Written | Listening</summary>
        public string ExamMode { get; set; } = "Written";

        /// <summary>是否為英聽考卷</summary>
        public bool IsListeningExam =>
            string.Equals(ExamMode, "Listening", StringComparison.OrdinalIgnoreCase);

        /// <summary>TTS 是否已設定（未設定時考卷仍可產生，但無音檔）</summary>
        public bool TtsConfigured { get; set; }

        /// <summary>未設定 TTS 或部分題目無音檔時的整體提示</summary>
        public string? TtsNoticeMessage { get; set; }

        /// <summary>整份英聽考卷合併後的 MP3 URL（含題號、間隔、重複念法）</summary>
        public string? ExamListeningAudioUrl { get; set; }

        /// <summary>英聽完整音檔下載檔名</summary>
        public string? ExamListeningAudioDownloadName { get; set; }
    }

    public class ScoreTable
    {
        public int WordScore { get; set; }
        public int PhraseScore { get; set; }
        public int MentalMathScore { get; set; }

        /// <summary>單字每題權重（單字:片語 = 2:3）</summary>
        public int wordWeight = 2;
        /// <summary>片語每題權重</summary>
        public int phraseWeight = 3;
        public int totalScore = 100;
    }


    public class ReExamSearchListViewModel_param
    {
        public List<string> selectedWordIDs { get; set; }
        public string KidID { get; set; }
        public string TestType { get; set; }
        public string CorrectType { get; set; }
        public string ClassName { get; set; }

        /// <summary>Written | Listening</summary>
        public string ExamMode { get; set; } = "Written";
    }
}
