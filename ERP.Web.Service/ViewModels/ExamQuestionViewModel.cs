using ERP.Web.Models.Models;
using ERP.Web.Utility.Paging;
using System.Web.Mvc;

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

        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
    }
    public class ExamSearchListViewModel_result : ExamSearchListViewModel_param
    {
        public string TestDate { get; set; }
        public List<ExamMainModel> ExamDataList { get; set; }
        public List<string> ClassNameList { get; set; }
        public List<SelectListItem> KidList { get; set; }
        public List<SelectListItem> TestDateList { get; set; }
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
    }

    public class ScoreTable
    {
        public int WordScore { get; set; }
        public int PhraseScore { get; set; }
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
