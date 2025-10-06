using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        public async Task<ControllerSettingDataMaintainViewModel_result> GetControllerSettingResultAsync(ControllerMainModel param)
        {
            var result = new ControllerSettingDataMaintainViewModel_result();

            // 取站點資料
            List<StationMainModel> stationData = await _controllerSettingRepo.GetStationMainDataAsync();
            // 站點下拉選單
            result.StationListItem = stationData.Select(s => new SelectListItem
            {
                Text = $"{s.StationCode} {s.StationName} {s.Domain}",
                Value = s.ID.ToString()
            }).ToList();

            // 控制器清單
            result.ControllerListItem = await GetControllerListItemAsync(param.StationMainID.ToString());
            // Icon 清單 & 分頁
            result.IconGroup = await GetIconGroup();
            return result;
        }

        public async Task<IconGroup> GetIconGroup(int page = 1, int pageSize = 100)
        {
            var result = new IconGroup();
            var datacount = await _controllerSettingRepo.GetIconCountAsync();
            var pager = new Paging(page, pageSize, datacount);
            var icons = await _controllerSettingRepo.GetIconList(pager);
            result.IconList = icons;
            result.Pager = pager;
            return result;
        }
        /// <summary>
        /// 建置控制器文字工具
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string BuildText(StationMainModel x)
        {
            var parts = new List<string>();

            // Controller/Action
            if (!string.IsNullOrEmpty(x.Action))
                parts.Add($"{x.StationCode} {x.Controller}/{x.Action}");
            else
                parts.Add(x.StationCode + " 最外層標籤 : ");

            // HttpMethod
            if (!string.IsNullOrEmpty(x.HttpMethod))
                parts.Add(x.HttpMethod);

            // DisplayName
            if (!string.IsNullOrEmpty(x.DisplayName))
                parts.Add(x.DisplayName);

            return string.Join(" ", parts);
        }
    }
}
