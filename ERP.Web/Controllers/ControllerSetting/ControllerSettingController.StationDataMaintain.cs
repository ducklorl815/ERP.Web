using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> StationDataMaintain()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> StationDataMaintain(StationDataMaintainViewModel_param param)
        {

            var result = await _controllerSettingService.StationDataMaintain(param);

            return View(result);
        }
    }
}
