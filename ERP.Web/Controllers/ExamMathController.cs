using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class ExamMathController : Controller
    {
        private readonly ExamService _examService;
        public ExamMathController(ExamService examService)
        {
            _examService = examService;
        }

        public async Task<IActionResult> NewTest(ExamSearchListViewModel_param param)
        {
            param.TestType = "Math";
            var result = await _examService.GetNewTestAsync(param);
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_NewTest.cshtml", result);
            return View("~/Views/Exam/NewTest.cshtml", result);
        }

        public async Task<IActionResult> ReTest(ExamSearchListViewModel_param param)
        {
            param.TestType = "Math";
            var result = await _examService.GetReTestAsync(param);
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_ReTest.cshtml", result);
            return View("~/Views/Exam/ReTest.cshtml", result);
        }

        public async Task<IActionResult> Test(ExamSearchListViewModel_param param)
        {
            param.TestType = "Math";
            var result = await _examService.GetExamDataAsync(param);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        public async Task<IActionResult> ReExam(ReExamSearchListViewModel_param param)
        {
            param.TestType = "Math";
            var result = await _examService.GetReExamDataAsync(param);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQuestions(int level, string KidID)
        {
            // 使用數學預設
            var result = await _examService.GenerateQuestions(level, KidID);
            return View("~/Views/Exam/Test.cshtml", result);
        }
    }
}


