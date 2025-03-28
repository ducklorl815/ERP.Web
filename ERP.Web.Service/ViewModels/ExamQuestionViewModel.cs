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
        public string ClassName { get; set; }
        public string CorrectType { get; set; }
        public string KidID { get; set; }

    }
    public class ExamSearchListViewModel_result : ExamSearchListViewModel_param
    {
        public List<ExamMainModel> ExamDataList { get; set; }
        public List<string> ClassNameList { get; set; }
        public List<SelectListItem> KidList { get; set; }
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
        public List<Vocabulary> VocabularyList { get; set; }
        public string Title { get; set; }
    }
}
