
using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        public async Task<IActionResult> ActDataMaintain(string ID)
        {
            var result = await _controllerSettingService.GetActDataMaintain(ID);

            return View("ControllerDataMaintain", result);
        }
        [HttpPost]
        public async Task<IActionResult> EditActDataMaintain(ControllerSettingDataMaintainViewModel_param param)
        {

            var result = await _controllerSettingService.UpdateActDataMaintain(param);
            return Json(result);
        }
    }
}
