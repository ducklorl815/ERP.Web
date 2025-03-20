using System.Web.Mvc;

namespace ERP.Web.Service.ViewModels
{
    public class ExamQuestionViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; } // 正確答案
    }
    public class ExamSearchListViewModel_result
    {
        public List<string> ClassNameList { get; set; }
    }
}
