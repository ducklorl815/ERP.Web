using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingActionListViewModel_result> ControllerSettingActionList(ControllerSettingActionListViewModel_param param)
        {
            var result = new ControllerSettingActionListViewModel_result();

            var actionParam = JsonConvert.DeserializeObject<ControllerSettingActionMainModel_param>(JsonConvert.SerializeObject(param));

            List<ControllerSettingActionMainModel> ActionList = await _controllerSettingRepo.ControllerSettingActionList(actionParam);

            result.ActionList = JsonConvert.DeserializeObject<List<ActionViewModel>>(JsonConvert.SerializeObject(ActionList));

            return result;
        }
    }
}
