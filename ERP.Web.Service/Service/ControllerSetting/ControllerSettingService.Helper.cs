using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Models.Models.Tools;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

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
            result.IconGroup = await GetIconGroup(new IconGroup_param());
            return result;
        }

        public async Task<IconGroup> GetIconGroup(IconGroup_param param)
        {
            var result = new IconGroup();
            IconGroupMain_param IconModel = JsonConvert.DeserializeObject<IconGroupMain_param>(JsonConvert.SerializeObject(param));
            var datacount = await _controllerSettingRepo.GetIconCountAsync(IconModel);
            IconModel.Pager = new Paging(param.Page, param.PageSize, datacount);

            var icons = await _controllerSettingRepo.GetIconList(IconModel);
            result.IconList = icons;
            result.Pager = IconModel.Pager;
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
            else if (x.ParentControllerMainID != Guid.Empty)
                parts.Add($"{x.StationCode} {x.ParentDisplayName} 的內層標籤 : ");
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
        public async Task<List<MenuData>> TreeView()
        {
            var roots = new List<MenuData>();
            var menuData = await _controllerSettingRepo.GetMenuDataAsync(); // 假設這是你原本抓資料的方法

            if (menuData == null || !menuData.Any())
                return roots;

            // ======= 以下重用你原本 LEFTSIDER 的邏輯 =======
            var flatMenuList = menuData
                .Where(s => s != null)
                .Select(s => new MenuData
                {
                    ID = s.ID,
                    ParentControllerMainID = s.ParentControllerMainID,
                    Controller = s.Controller ?? string.Empty,
                    Action = s.Action ?? string.Empty,
                    DisplayName = string.IsNullOrEmpty(s.DisplayName) ? s.Controller ?? "" : s.DisplayName,
                    IconClass = string.IsNullOrEmpty(s.IconClass) ? "" : s.IconClass,
                    Sort = s.Sort,
                    IsMenu = s.IsMenu,
                    Children = new List<MenuData>(),
                    IsActive = false,
                    Domain = "https://localhost:44372/",
                    IsBlank = s.IsBlank,
                    Enabled = s.Enabled,
                    Deleted = s.Deleted
                })
                .ToList();

            var lookup = flatMenuList.ToDictionary(x => x.ID);

            foreach (var item in flatMenuList)
            {
                if (item.ParentControllerMainID == Guid.Empty)
                    roots.Add(item);
                else if (lookup.TryGetValue(item.ParentControllerMainID, out var parent))
                    parent.Children.Add(item);
            }

            return roots;
        }

        public async Task<List<ErpMenuData>> ErpTreeView()
        {
            var roots = new List<ErpMenuData>();
            var erpMenuData = await _controllerSettingRepo.GetErpMenuDataAsync();

            if (erpMenuData == null || !erpMenuData.Any())
                return roots;

            // ======= 以下重用你原本 LEFTSIDER 的邏輯 =======
            var flatMenuList = erpMenuData
                .Where(s => s != null)
                .Select(s => new ErpMenuData
                {
                    ID = s.ID,
                    ControllerMainID = s.ControllerMainID,
                    Level = s.Level,
                    ControllerDesc = s.ControllerDesc,
                    ControllerName = s.ControllerName ?? string.Empty,
                    ActName = s.ActName ?? string.Empty,
                    Name = string.IsNullOrEmpty(s.Name) ? s.ControllerName ?? "" : s.Name,
                    IconClass = string.IsNullOrEmpty(s.IconClass) ? "" : s.IconClass,
                    Sort = s.Sort,
                    IsMenu = s.IsMenu,
                    Children = new List<ErpMenuData>(),
                    IsActive = false,
                    Domain = "https://localhost:44372/",
                    Enabled = s.Enabled,
                    Deleted = s.Deleted
                })
                .ToList();

            var lookup = flatMenuList.ToDictionary(x => x.ID);

            foreach (var item in flatMenuList)
            {
                if (item.ControllerMainID == Guid.Empty)
                    roots.Add(item);
                else if (lookup.TryGetValue(item.ControllerMainID, out var parent))
                    parent.Children.Add(item);
            }

            return roots;
        }
    }
}
