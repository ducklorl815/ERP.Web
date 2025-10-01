using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.Models;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {

        public async Task<StationDataMaintainViewModel_result> StationDataMaintain(StationDataMaintainViewModel_param param)
        {
            var result = new StationDataMaintainViewModel_result();

            var StationData = JsonConvert.DeserializeObject<StationMainModel>(JsonConvert.SerializeObject(param));

            result.IsSuccess = await _controllerSettingRepo.StationDataMaintain(StationData);

            return result;
        }

    }
}
