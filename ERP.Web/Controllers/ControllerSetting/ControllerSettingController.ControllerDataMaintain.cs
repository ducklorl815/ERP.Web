using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ControllerDataMaintain()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ControllerDataMaintain(ControllerSettingDataMaintainViewModel_param param)
        {

            var result = await _controllerSettingService.ControllerDataMaintain(param);

            return View(result);
        }
    }
}
