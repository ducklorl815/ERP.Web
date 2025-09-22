using LifeTech.FlashWolf.ERP.Web.Models.Repo.ControllerSetting;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        private readonly ControllerSettingRepo _controllerSettingRepo;
        public ControllerSettingService
            (
            ControllerSettingRepo controllerSettingRepo
            )
        {
            _controllerSettingRepo = controllerSettingRepo;
        }
    }
}
