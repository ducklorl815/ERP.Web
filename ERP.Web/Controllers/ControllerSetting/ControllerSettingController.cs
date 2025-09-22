using ERP.Web.Service.Service.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        private readonly ControllerSettingService _controllerSettingService;
        public ControllerSettingController
            (
            ControllerSettingService controllerSettingService
            )
        {
            _controllerSettingService = controllerSettingService;
        }
    }
}
