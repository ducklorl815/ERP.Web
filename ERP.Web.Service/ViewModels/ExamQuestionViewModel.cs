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
        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
        /// <summary>關鍵字：課程、問題、答案一併搜尋</summary>
        public string SearchText { get; set; }
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
                //new SelectListItem { Text = "複習錯誤試題", Value = "1" }
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
    }

    public class ScoreTable
    {
        public int WordScore { get; set; }
        public int PhraseScore { get; set; }
        public int MentalMathScore { get; set; }

        public double wordWeight = 0.3;
        public double phraseWeight = 0.7;
        public int totalScore = 100;
    }


    public class ReExamSearchListViewModel_param
    {
        public List<string> selectedWordIDs { get; set; }
        public string KidID { get; set; }
        public string TestType { get; set; }
        public string CorrectType { get; set; }
        public string ClassName { get; set; }
    }
}
