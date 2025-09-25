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


            var MainData = new ControllerMainModel
            {
                Controller = param?.Controller ?? string.Empty,
                ParentControllerMainID = param?.ParentControllerMainID != Guid.Empty
                                ? param.ParentControllerMainID
                                : Guid.Empty,
                IconClass = param?.IconClass ?? string.Empty,
                ControllerDesc = string.IsNullOrEmpty(param?.ControllerDesc)
                                ? string.Empty
                                : param.ControllerDesc,
                ControllerActionID = await _controllerSettingRepo.ControllerActionDataMaintain(param.ActionName),
                DisplayName = param?.DisplayName ?? string.Empty,
                FrontNumber = string.IsNullOrEmpty(param?.FrontNumber)
                                ? string.Empty
                                : param.FrontNumber,
                HttpMethod = param?.HttpMethod ?? string.Empty,
                IsBlank = param?.IsBlank ?? false,
                IsMenu = param?.IsMenu ?? false,
                Level = param?.Level ?? 0,
                StationMainID = param?.StationMainID != Guid.Empty
                                ? param.StationMainID
                                : Guid.Empty,
                PageNumber = param?.PageNumber ?? 0,
                Sort = param?.Sort ?? 0,
                AbandonReason = string.Empty
            };


            result.IsSuccess = await _controllerSettingRepo.ControllerDataMaintain(MainData);
            return result;

        }

        public async Task<string?> StationDataMaintain(StationDataMaintainViewModel_param param)
        {
            throw new NotImplementedException();
        }
    }
}
