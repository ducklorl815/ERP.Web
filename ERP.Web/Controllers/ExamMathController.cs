using ERP.Web.Helpers;
using ERP.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    /// <summary>
    /// 數學考試控制器：加減乘除出題平台
    /// </summary>
    public class ExamMathController : Controller
    {
        /// <summary>
        /// 數學出題平台首頁（加法、減法、乘法、除法、綜合）
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/ExamMath/Index.cshtml", new MathPlatformIndexViewModel());
        }

        /// <summary>
        /// 產生考卷（含答案卷）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GeneratePaper(MathPaperInputViewModel input)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/ExamMath/Index.cshtml", MathPlatformIndexViewModel.FromInput(input));
            }

            if (input.OperationType == MathOperationType.Mixed
                && !input.IncludeAddition
                && !input.IncludeSubtraction
                && !input.IncludeMultiplication
                && !input.IncludeDivision)
            {
                ModelState.AddModelError(string.Empty, "綜合出題請至少勾選一種運算類型。");
                return View("~/Views/ExamMath/Index.cshtml", MathPlatformIndexViewModel.FromInput(input));
            }

            var paper = MathPaperHelper.BuildPaper(input);
            if (paper.Questions.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "依目前條件無法產生題目，請調整數字範圍或題數設定。");
                return View("~/Views/ExamMath/Index.cshtml", MathPlatformIndexViewModel.FromInput(input));
            }

            return View("~/Views/ExamMath/Paper.cshtml", paper);
        }

        /// <summary>
        /// 99 乘法考卷（保留舊網址相容）
        /// </summary>
        [HttpGet]
        public IActionResult TimesTable(
            string? multiplicand = null,
            string? multiplier = null,
            int questionCount = 0,
            bool shuffle = true,
            int? seed = null,
            string? title = null)
        {
            var input = new MathPaperInputViewModel
            {
                OperationType = MathOperationType.Multiplication,
                LeftOperand = string.IsNullOrWhiteSpace(multiplicand) ? "1-9" : multiplicand,
                RightOperand = string.IsNullOrWhiteSpace(multiplier) ? "1-9" : multiplier,
                QuestionCount = questionCount,
                Shuffle = shuffle,
                Seed = seed,
                Title = title
            };

            var paper = MathPaperHelper.BuildPaper(input);
            if (paper.Questions.Count == 0)
            {
                return BadRequest("依目前條件無法產生題目，請檢查被乘數與乘數設定。");
            }

            return View("~/Views/ExamMath/Paper.cshtml", paper);
        }
    }
}
