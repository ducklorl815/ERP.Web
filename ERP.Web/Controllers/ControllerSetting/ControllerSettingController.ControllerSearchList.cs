using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        public async Task<IActionResult> ControllerSettingSearchList(ControllerSettingSearchListViewModel_param param)
        {
            var result = await _controllerSettingService.ControllerSettingSearchList(param);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ControllerSettingSearchList", result);
            }
            else
            {
                return View(result);
            }

        }


    }
}
