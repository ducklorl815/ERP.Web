using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public partial class ExamController : Controller
    {
        private readonly ExamService _examService;
        public ExamController(ExamService examService)
        {
            _examService = examService;
        }
    }
}