using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ControllerDataMaintain()
        {
            var result = await _controllerSettingService.GetControllerDataMaintain();
            return View(result);
        }
        [HttpPost]
        public async Task<IActionResult> ControllerDataMaintain(ControllerSettingDataMaintainViewModel_param param)
        {

            var result = await _controllerSettingService.ControllerDataMaintain(param);
            return Json(new { success = false });
        }
        [HttpGet]
        public async Task<IActionResult> GetControllerListItem(string StationMainID)
        {

            var result = await _controllerSettingService.GetControllerListItemAsync(StationMainID);
            return Json(result);
        }
    }
}
