
using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        public async Task<IActionResult> ControllerSettingActionSearchList(ControllerSettingActionListViewModel_param param)
        {
            var result = await _controllerSettingService.ControllerSettingActionList(param);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ControllerSettingActionSearchList", result);
            }
            else
            {
                return View(result);
            }
        }
    }
}
