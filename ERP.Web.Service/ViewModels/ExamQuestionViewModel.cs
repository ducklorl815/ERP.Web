using ERP.Web.Models.Models;
using LifeTech.ERP.Utility.Paging;
using System.Net.NetworkInformation;
using System.Web.Mvc;

namespace ERP.Web.Service.ViewModels
{
    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
    }
    public class ExamSearchListViewModel_param
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 15;


        public Paging Pager { get; set; }
    }
    public class ExamSearchListViewModel_result
    {
        public List<string> ClassNameList { get; set; }
        public List<SelectListItem> KidList { get; set; }

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
