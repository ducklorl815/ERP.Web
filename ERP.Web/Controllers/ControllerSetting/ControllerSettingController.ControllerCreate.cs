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
        public async Task<IActionResult> ControllerCreate(ControllerSettingDataMaintainViewModel_param param)
        {

            var result = await _controllerSettingService.ControllerCreate(param);
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetControllerListItem(string StationMainID)
        {

            var result = await _controllerSettingService.GetControllerListItemAsync(StationMainID);
            return Json(result);
        }

        public async Task<IActionResult> IconList(int page = 1, int pageSize = 100)
        {

            var result = await _controllerSettingService.GetIconList(page, pageSize);

            return PartialView("_IconListPartial", result);
        }
    }
}
