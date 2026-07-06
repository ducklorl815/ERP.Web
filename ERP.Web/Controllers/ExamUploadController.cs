using ERP.Web.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class ExamUploadController : Controller
    {
        private readonly ExamService _examService;

        public ExamUploadController(ExamService examService)
        {
            _examService = examService;
        }

        // 上傳 Excel 首頁
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/ExamUpload/Index.cshtml");
        }

        // 接收 Excel 上傳
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("請上傳 Excel 檔案");
            }

            var chkUpload = await _examService.GetUploadFileAsync(file);
            return Json("true");
        }

        /// <summary>
        /// 補救：用 Excel 的 (Answer→Question) 回填 Vocabulary.Question。
        /// 預覽：不寫入 DB，只回傳命中/重複/預計更新筆數。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RestoreQuestionPreview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("請上傳 Excel 檔案");

            var preview = await _examService.PreviewRestoreQuestionFromExcelAsync(file, onlyWhenQuestionEqualsAnswer: true);
            return Json(preview);
        }

        /// <summary>
        /// 補救：用 Excel 的 (Answer→Question) 回填 Vocabulary.Question。
        /// 套用：實際更新 DB（預設僅更新 Question==Answer 的資料，避免覆蓋已正確 Question）。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RestoreQuestionApply(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("請上傳 Excel 檔案");

            var result = await _examService.RestoreQuestionFromExcelAsync(file, onlyWhenQuestionEqualsAnswer: true);
            return Json(result);
        }
    }
}


