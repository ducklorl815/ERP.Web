using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Service.ViewModels.Tools;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetControllerListItem(string StationMainID)
        {

            var result = await _controllerSettingService.GetControllerListItemAsync(StationMainID);
            return Json(result);
        }

        public async Task<IActionResult> IconList(IconGroup_param param)
        {
            var result = await _controllerSettingService.GetIconGroup(param);

            if (Request.IsAjaxRequest())
                return PartialView("_IconListPartialData", result);

            return PartialView("_IconListPartial", result);
        }
    }
}
