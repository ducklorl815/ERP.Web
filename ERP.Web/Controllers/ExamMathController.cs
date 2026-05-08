using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    /// <summary>
    /// 數學考試控制器
    /// 處理所有數學相關的考試功能
    /// </summary>
    public class ExamMathController : Controller
    {
        private readonly ExamService _examService;
        private const string TestType = "Math"; // 固定為 Math

        public ExamMathController(ExamService examService)
        {
            _examService = examService;
        }

        #region 考試頁面

        /// <summary>
        /// 新測驗頁面（尚未考過的）
        /// </summary>
        public async Task<IActionResult> NewTest(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.GetNewTestAsync(param);
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_NewTest.cshtml", result);
            return View("~/Views/Exam/NewTest.cshtml", result);
        }

        /// <summary>
        /// 複習測驗頁面（已考過的）
        /// </summary>
        public async Task<IActionResult> ReTest(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.GetReTestAsync(param);
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_ReTest.cshtml", result);
            return View("~/Views/Exam/ReTest.cshtml", result);
        }

        /// <summary>
        /// 新測驗考試頁面
        /// </summary>
        public async Task<IActionResult> Test(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.GetExamDataAsync(param);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        /// <summary>
        /// 複習考試頁面
        /// </summary>
        public async Task<IActionResult> ReExam(ReExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.GetReExamDataAsync(param);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        /// <summary>
        /// 產生心算題目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateQuestions(int level, string KidID)
        {
            var result = await _examService.GenerateQuestions(level, KidID);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        #endregion

        #region AJAX 更新方法

        /// <summary>
        /// 更新考試題目的對錯狀態
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateExamWord(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.UpdateExamWord(param);
            return Json(result);
        }

        /// <summary>
        /// 更新新測驗題目（Focus 標記和題目內容）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateNewTestWord(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.UpdateNewTestWord(param);
            return Json(result);
        }

        /// <summary>
        /// 取得學生的考試日期清單
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetTestDates(string KidID)
        {
            var result = await _examService.GetTestDateList(KidID, TestType);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetClassesByDate(string KidID, string TestDate)
        {
            var result = await _examService.GetClassNameListByDate(KidID, TestDate, TestType);
            return Json(result);
        }

        #endregion
    }
}

