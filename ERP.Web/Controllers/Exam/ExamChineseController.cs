using ERP.Web.Service.Service.Exam;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Exam
{
    /// <summary>
    /// 國語考試控制器
    /// 處理所有國語相關的考試功能
    /// </summary>
    public class ExamChineseController : Controller
    {
        private readonly ExamService _examService;

        public ExamChineseController(ExamService examService)
        {
            _examService = examService;
        }

        #region 寫生字練習紙

        /// <summary>
        /// 顯示寫生字練習紙輸入表單
        /// </summary>
        [HttpGet]
        public IActionResult CharacterPracticeForm()
        {
            var result = new CharacterPracticeViewModel
            {
                Characters = string.Empty
            };
            return View("~/Views/Exam/CharacterPracticeForm.cshtml", result);
        }

        /// <summary>
        /// 產生寫生字練習紙
        /// </summary>
        [HttpPost]
        public IActionResult GenerateCharacterPracticeSheet(string characters)
        {
            if (string.IsNullOrWhiteSpace(characters))
            {
                TempData["ErrorMessage"] = "請輸入要練習的生字！";
                return RedirectToAction("CharacterPracticeForm");
            }

            // 移除空白和重複字元
            var uniqueCharacters = new System.Text.StringBuilder();
            var seen = new HashSet<char>();
            
            foreach (var c in characters)
            {
                // 只保留中文字元（Unicode 範圍：\u4E00-\u9FFF）
                if (c >= 0x4E00 && c <= 0x9FFF && !seen.Contains(c))
                {
                    uniqueCharacters.Append(c);
                    seen.Add(c);
                }
            }

            if (uniqueCharacters.Length == 0)
            {
                TempData["ErrorMessage"] = "請輸入有效的中文字元！";
                return RedirectToAction("CharacterPracticeForm");
            }

            var result = new CharacterPracticeSheetViewModel
            {
                Characters = uniqueCharacters.ToString(),
                Title = $"寫生字練習紙_{DateTime.Now:yyyyMMdd}"
            };

            return View("~/Views/Exam/_CharacterPracticeSheet.cshtml", result);
        }

        #endregion
    }

    /// <summary>
    /// 寫生字練習表單 ViewModel
    /// </summary>
    public class CharacterPracticeViewModel
    {
        public string Characters { get; set; } = string.Empty;
    }

    /// <summary>
    /// 寫生字練習紙 ViewModel
    /// </summary>
    public class CharacterPracticeSheetViewModel
    {
        public string Characters { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}

