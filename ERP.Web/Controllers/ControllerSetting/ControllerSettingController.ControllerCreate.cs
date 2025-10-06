using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ControllerCreate()
        {
            var result = await _controllerSettingService.ControllerCreate();
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> ControllerCreate(ControllerSettingCreateViewModel_param param)
        {

            var result = await _controllerSettingService.ControllerCreate(param);
            return Json(result);
        }
    }
}
