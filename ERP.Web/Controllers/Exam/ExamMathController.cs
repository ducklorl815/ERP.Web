using ERP.Web.Models.Models;
using ERP.Web.Service.Service.Exam;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Exam
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
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_MathNewTest.cshtml", result);
            return View("~/Views/Exam/MathNewTest.cshtml", result);
        }

        /// <summary>
        /// 複習測驗頁面（已考過的）
        /// </summary>
        public async Task<IActionResult> ReTest(ExamSearchListViewModel_param param)
        {
            param.TestType = TestType;
            var result = await _examService.GetReTestAsync(param);
            if (Request.IsAjaxRequest()) return PartialView("~/Views/Exam/_MathReTest.cshtml", result);
            return View("~/Views/Exam/MathReTest.cshtml", result);
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
        /// 產生心算題目（舊版）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateQuestions(int level, string KidID)
        {
            var result = await _examService.GenerateQuestions(level, KidID);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        /// <summary>
        /// 產生珠心算題目（新版：可自定義位數）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateMentalMathQuestions(
            string KidID,
            int questionCount,
            int digitCount,
            int numberCount,
            bool allowNegativeResult = false)
        {
            var result = await _examService.GenerateMentalMathQuestions(
                KidID, questionCount, digitCount, numberCount, allowNegativeResult);
            return View("~/Views/Exam/Test.cshtml", result);
        }

        #endregion

        #region 四則運算測驗

        /// <summary>
        /// 顯示加法測驗頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AdditionTest()
        {
            var kidList = await _examService.GetKidList();
            var result = new ExamDataViewModel_result
            {
                VocabularyList = new List<Vocabulary>(),
                scoreTable = new ScoreTable(),
                Title = string.Empty,
                KidList = kidList
            };
            return View("~/Views/Exam/AdditionTest.cshtml", result);
        }

        /// <summary>
        /// 產生加法題目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateAdditionQuestions(
            string KidID,
            int questionCount,
            int firstNumberDigits,
            int addendDigits,
            int numberCount,
            int resultDigitCount)
        {
            var result = await _examService.GenerateAdditionQuestions(
                KidID, questionCount, firstNumberDigits, addendDigits, numberCount, resultDigitCount);
            return View("~/Views/Exam/_ArithmeticTest.cshtml", result);
        }

        /// <summary>
        /// 顯示減法測驗頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SubtractionTest()
        {
            var kidList = await _examService.GetKidList();
            var result = new ExamDataViewModel_result
            {
                VocabularyList = new List<Vocabulary>(),
                scoreTable = new ScoreTable(),
                Title = string.Empty,
                KidList = kidList
            };
            return View("~/Views/Exam/SubtractionTest.cshtml", result);
        }

        /// <summary>
        /// 產生減法題目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateSubtractionQuestions(
            string KidID,
            int questionCount,
            int minuendDigits,
            int subtrahendDigits,
            int numberCount,
            int resultDigitCount,
            bool allowNegative = false)
        {
            var result = await _examService.GenerateSubtractionQuestions(
                KidID, questionCount, minuendDigits, subtrahendDigits, numberCount, resultDigitCount, allowNegative);
            return View("~/Views/Exam/_ArithmeticTest.cshtml", result);
        }

        /// <summary>
        /// 顯示乘法測驗頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MultiplicationTest()
        {
            var kidList = await _examService.GetKidList();
            var result = new ExamDataViewModel_result
            {
                VocabularyList = new List<Vocabulary>(),
                scoreTable = new ScoreTable(),
                Title = string.Empty,
                KidList = kidList
            };
            return View("~/Views/Exam/MultiplicationTest.cshtml", result);
        }

        /// <summary>
        /// 產生乘法題目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateMultiplicationQuestions(
            string KidID,
            int questionCount,
            int multiplicandDigits,
            int multiplierDigits,
            int resultDigitCount)
        {
            var result = await _examService.GenerateMultiplicationQuestions(
                KidID, questionCount, multiplicandDigits, multiplierDigits, resultDigitCount);
            return View("~/Views/Exam/_ArithmeticTest.cshtml", result);
        }

        /// <summary>
        /// 顯示除法測驗頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DivisionTest()
        {
            var kidList = await _examService.GetKidList();
            var result = new ExamDataViewModel_result
            {
                VocabularyList = new List<Vocabulary>(),
                scoreTable = new ScoreTable(),
                Title = string.Empty,
                KidList = kidList
            };
            return View("~/Views/Exam/DivisionTest.cshtml", result);
        }

        /// <summary>
        /// 產生除法題目
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateDivisionQuestions(
            string KidID,
            int dividendDigits,
            int divisorDigits,
            int exactCount,
            int remainderCount)
        {
            var result = await _examService.GenerateDivisionQuestions(
                KidID, dividendDigits, divisorDigits, exactCount, remainderCount);
            return View("~/Views/Exam/_ArithmeticTest.cshtml", result);
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
            var result = await _examService.GetTestDateList(KidID);
            return Json(result);
        }

        #endregion
    }
}

