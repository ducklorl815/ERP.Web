using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingSearchListViewModel_result> ControllerSettingSearchList(ControllerSettingSearchListViewModel_param param)
        {
            var result = new ControllerSettingSearchListViewModel_result()
            {
                ControllerList = new List<ControllerListMainModel>()
            };

            result.ControllerList = await _controllerSettingRepo.ControllerSettingSearchList();

            return result;
        }
    }
}
