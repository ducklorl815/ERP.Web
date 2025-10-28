using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    /// <summary>
    /// 英文考試控制器
    /// 處理所有英文相關的考試功能
    /// </summary>
    public class ExamEnglishController : Controller
    {
        private readonly ExamService _examService;
        private const string TestType = "English"; // 固定為 English

        public ExamEnglishController(ExamService examService)
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

        #endregion

        #region AJAX 更新方法

        /// <summary>
        /// 更新考試單字的對錯狀態
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateExamWord(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.UpdateExamWord(param);
            return Json(result);
        }

        /// <summary>
        /// 更新新測驗單字（Focus 標記和單字內容）
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
            var result = await _examService.GetTestDateList(KidID);
            return Json(result);
        }

        #endregion
    }
}

