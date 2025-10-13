using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Service.ViewModels.Tools;
using ERP.Web.Utility.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingCreateViewModel_result> ControllerCreate()
        {
            var result = new ControllerSettingCreateViewModel_result()
            {
                ControllerMain = new ControllerMainModel()
            };

            List<StationMainModel> stationDataTask = await _controllerSettingRepo.GetStationMainDataAsync();

            result.ControllerListItem = await GetControllerListItemAsync("");
            result.IconGroup = await GetIconGroup(new IconGroup_param());
            result.StationListItem = stationDataTask.Select(s => new SelectListItem
            {
                Text = $"{s.StationCode} {s.StationName} {s.Domain}",
                Value = s.ID.ToString()
            }).ToList();

            return result;
        }

        public async Task<ControllerSettingCreateViewModel_result> ControllerCreate(ControllerSettingCreateViewModel_param param)
        {
            var result = new ControllerSettingCreateViewModel_result()
            {
                IsSuccess = false
            };

            var MainData = JsonConvert.DeserializeObject<ControllerMainModel>(JsonConvert.SerializeObject(param.ControllerMain));

            Guid InsertID = await _controllerSettingRepo.ControllerCreate(MainData);
            if (InsertID != Guid.Empty)
            {
                result.IsSuccess = true;
                result.ID = InsertID.ToString();
            }
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

            ControllerMainModel ActData = await _controllerSettingRepo.GetActDataMaintain(ID);

            var result = await GetControllerSettingResultAsync(ActData);

            result.ControllerMain = ActData;

            return result;
        }
        public async Task<ControllerSettingDataMaintainViewModel_result> UpdateActDataMaintain(ControllerSettingDataMaintainViewModel_param param)
        {
            var result = new ControllerSettingDataMaintainViewModel_result();
            var ControllerData = JsonConvert.DeserializeObject<ControllerMainModel>(JsonConvert.SerializeObject(param.ControllerMain));
            var chkUpdate = await _controllerSettingRepo.UpdateActDataMaintain(ControllerData);
            result.IsSuccess = chkUpdate;
            return result;
        }
    }
}
