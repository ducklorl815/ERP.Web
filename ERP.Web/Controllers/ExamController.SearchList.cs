using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;



namespace ERP.Web.Controllers
{
    public partial class ExamController : Controller
    {
        public async Task<IActionResult> ReTest(ExamSearchListViewModel_param param)
        {
            param.CorrectType = "1";
            ExamSearchListViewModel_result result = await _examService.GetIndexAsync(param);
            if (Request.IsAjaxRequest())
                return PartialView("_ReTest", result);
            else
                return View(result);

        }
        public async Task<IActionResult> NewTest(ExamSearchListViewModel_param param)
        {
            param.CorrectType = "0";
            ExamSearchListViewModel_result result = await _examService.GetIndexAsync(param);
            return View(result);
        }

        public async Task<IActionResult> GetList(ExamSearchListViewModel_param param)
        {

            var result = await _examService.GetListAsync(param);
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ReTest", result);
            }
            else
            {
                return View(result);
            }
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("請上傳 Excel 檔案");
            }
            var chkUpload = await _examService.GetUploadFileAsync(file);
            return Json("true");
        }
        public async Task<IActionResult> Test(ExamSearchListViewModel_param param)
        {

            var result = await _examService.GetExamDataAsync(param);

            return View(result);
        }

    }

}