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
    }
}


