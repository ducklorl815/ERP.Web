using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.Models;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingDataMaintainViewModel_result> ControllerCreate()
        {
            var result = new ControllerSettingDataMaintainViewModel_result();

            List<StationMainModel> stationDataTask = await _controllerSettingRepo.GetStationMainDataAsync();

            result.ControllerListItem = await GetControllerListItemAsync("");
            var IconList = await GetIconList();
            result.IconList = IconList.IconList;
            result.Pager = IconList.Pager;

            result.StationListItem = stationDataTask.Select(s => new SelectListItem
            {
                Text = $"{s.StationCode} {s.StationName} {s.Domain}",
                Value = s.ID.ToString()
            }).ToList();

            return result;

        }

        public async Task<ControllerSettingDataMaintainViewModel_result> ControllerCreate(ControllerSettingDataMaintainViewModel_param param)
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
                Action = param?.Action ?? string.Empty,
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
            var IconList = await GetIconList();
            result.IconList = IconList.IconList;

            result.IsSuccess = await _controllerSettingRepo.ControllerCreate(MainData);
            return result;

        }
        /// <summary>
        /// 取得控制器資訊
        /// </summary>
        /// <param name="Domain"></param>
        /// <returns></returns>
        public async Task<List<SelectListItem>> GetControllerListItemAsync(string StationMainID)
        {
            var result = new List<SelectListItem>();
            result.Add(new SelectListItem
            {
                Text = "新增最外層標籤",
                Value = "00000000-0000-0000-0000-000000000000"
            });
            List<StationMainModel> controllerDataTask = await _controllerSettingRepo.GetControllerDataAsync(StationMainID);
            result.AddRange(controllerDataTask.Select(x => new SelectListItem
            {
                Text = BuildText(x),
                Value = x.ID.ToString()
            }));

            return result;
        }

        public async Task<ControllerSettingSearchListViewModel_result> ControllerSettingSearchList(ControllerSettingSearchListViewModel_param param)
        {
            var result = new ControllerSettingSearchListViewModel_result()
            {
                ControllerList = new List<ControllerListMainModel>()
            };

            result.ControllerList = await _controllerSettingRepo.ControllerSettingSearchList();

            return result;
        }

        public async Task<ControllerSettingDataMaintainViewModel_result> GetActDataMaintain(string ID)
        {
            var result = new ControllerSettingDataMaintainViewModel_result();
            ControllerMainModel ActData = await _controllerSettingRepo.GetActDataMaintain(ID);

            result = await GetControllerSettingResultAsync(ActData);

            result.ControllerMain = ActData;

            return result;  
        }
    }
}
