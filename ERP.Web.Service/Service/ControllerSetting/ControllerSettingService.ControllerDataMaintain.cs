using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingDataMaintainViewModel_result> ControllerDataMaintain(ControllerSettingDataMaintainViewModel_param param)
        {

            var result = new ControllerSettingDataMaintainViewModel_result()
            {
                IsSuccess = false
            };
            var XXX = "{\"StationMainID\":\"14efb7e1-b978-4c68-9b17-72acddc26fed\",\"DisplayName\":\"站台製作\",\"Controller\":\"ControllerSetting\",\"ActionName\":\"ControllerDataMaintain\",\"HttpMethod\":\"Post\",\"ParentControllerMainID\":\"00000000-0000-0000-0000-000000000000\",\"IconClass\":\"\",\"FrontNumber\":\"PB\",\"Sort\":1,\"IsMenu\":false,\"isBlank\":1}";
            var YYY = JsonConvert.DeserializeObject<ControllerSettingDataMaintainViewModel_param>(XXX);

            var ZZZ = new ControllerMainModel
            {
                Controller = YYY.Controller,
                ParentControllerMainID = YYY.ParentControllerMainID,
                IconClass = YYY.IconClass,
                ControllerDesc = string.IsNullOrEmpty(YYY.ControllerDesc) ? "" : YYY.ControllerDesc,
                ControllerActionID = await _controllerSettingRepo.ControllerActionDataMaintain(YYY.ActionName),
                DisplayName = YYY.DisplayName,
                FrontNumber = YYY.FrontNumber,
                HttpMethod = YYY.HttpMethod,
                IsBlank = YYY.IsBlank,
                IsMenu = YYY.IsMenu,
                Level = YYY.Level,
                StationMainID = YYY.StationMainID
            };


            result.IsSuccess = await _controllerSettingRepo.ControllerDataMaintain(ZZZ);
            return result;

        }

        public async Task<string?> StationDataMaintain(StationDataMaintainViewModel_param param)
        {
            throw new NotImplementedException();
        }
    }
}
