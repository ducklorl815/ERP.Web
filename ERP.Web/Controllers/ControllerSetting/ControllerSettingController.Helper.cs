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

        public async Task<IActionResult> IconList(int page = 1, int pageSize = 100)
        {

            var result = await _controllerSettingService.GetIconGroup(page, pageSize);

            return PartialView("_IconListPartial", result);
        }
    }
}
