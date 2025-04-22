using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;



namespace ERP.Web.Controllers
{
    public partial class ExamController : Controller
    {
        public async Task<IActionResult> UpdateExamWord(ExamSearchListViewModel_param param)
        {
            var result = await _examService.UpdateExamWord(param);

            return Json(result);

        }
        public async Task<IActionResult> UpdateFocusWord(ExamSearchListViewModel_param param)
        {
            var result = await _examService.UpdateFocusWord(param);

            return Json(result);

        }
    }

}