
using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        public async Task<IActionResult> ControllerSettingActionList(ControllerSettingSearchListViewModel_param param)
        {
            var result = await _controllerSettingService.ControllerSettingActionList(param);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ControllerSettingActionList", result);
            }
            else
            {
                return View();
            }

        }


    }
}
