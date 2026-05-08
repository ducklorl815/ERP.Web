using ERP.Web.Utility.Models;
using ERP.Web.Utility.Respository;
using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace ERP.Web.Utility.ViewComponents
{
    public class LeftSidebarViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;
        private ILogger _logger;
        private readonly ControllerUtilityRepo _controllerUtilityRepo;
        private readonly string _connStr;

        public LeftSidebarViewComponent(
            IConfiguration configuration
            , ILogger<LeftSidebarViewComponent> logger
            , IOptions<DBList> options
            )
        {
            _controllerUtilityRepo = new ControllerUtilityRepo(configuration.GetConnectionString("UtilityERP"));
            _configuration = configuration;
            _logger = logger;
            _connStr = options.Value.erp;
        }
        #region 為了Utility而做的功能
        private class TreeNodeUtilityModel
        {
            public Guid ID { get; set; }      // 對應 JSON id
            public string DisplayName { get; set; }  // 對應 JSON text
            public List<TreeNodeUtilityModel> Children { get; set; } // 對應 JSON children
        }
        private class NodeRecordUtilityModel
        {
            public Guid ID { get; set; }
            public string DisplayName { get; set; }
            public Guid ParentControllerMainID { get; set; }
        }
        private async Task<List<string>> UtilityFlattenIDs(List<TreeNodeUtilityModel> nodes)
        {
            var result = new List<string>();

            foreach (var node in nodes)
            {
                result.Add(node.ID.ToString());

                if (node.Children != null && node.Children.Any())
                {
                    result.AddRange(await UtilityFlattenIDs(node.Children));
                }
            }

            return result;
        }
        #endregion

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = new LeftSidebarViewModel();
            var currentDomain = $"{Request.Host}/";
            var currentProject = string.Empty;
            var currentController = (string)ViewContext.RouteData.Values["Controller"];
            var currentAction = (string)ViewContext.RouteData.Values["Action"];

            var userMenuKey = _configuration.GetSection("RedisSessionKey:UserMenu").Value;

            // 測試階段：允許透過設定繞過權限與選單資料庫查詢，避免資料庫尚未就緒造成整站無法使用
            if (_configuration.GetValue<bool>("Permission:Bypass"))
            {
                return View(result);
            }

            string NodeJson = await _controllerUtilityRepo.GetBoundAccessGroupData(HttpContext.User.Identity.Name);
            IEnumerable<dynamic> menuData = null;
            List<string> ControllerIDList = new List<string>(); 
            try
            {
                if (!string.IsNullOrEmpty(NodeJson))
                {

                    List<TreeNodeUtilityModel> nodes = JsonConvert.DeserializeObject<List<TreeNodeUtilityModel>>(NodeJson);
                    ControllerIDList = await UtilityFlattenIDs(nodes);
                }

                menuData = await _controllerUtilityRepo.GetMenuDataAsync(ControllerIDList);
            }
            catch (Exception)
            {

            }

            //HttpContext.Session.SetString(userMenuKey, JsonConvert.SerializeObject(menuData));

            #region MyRegion
            //var userMenuMainSessionData = HttpContext.Session.GetString(userMenuKey);

            //if (string.IsNullOrEmpty(userMenuMainSessionData) || userMenuMainSessionData == "[]")
            //{
            //    menuData = await _controllerSettingRepo.GetMenuDataAsync(HttpContext.User.Identity.Name);

            //    HttpContext.Session.SetString(userMenuKey, JsonConvert.SerializeObject(menuData));
            //}
            //else
            //{
            //    menuData = JsonConvert.DeserializeObject<IEnumerable<ControllerMainTableModel>>(userMenuMainSessionData);
            //}
            #endregion


            if (menuData == null || !menuData.Any())
                return View(result);

            // 2️ 轉成平坦 MenuData 並防空
            var flatMenuList = menuData
                .Where(s => s != null)
                .Select(s => new MenuData
                {
                    ID = s.ID,
                    ParentControllerMainID = s.ParentControllerMainID,
                    Controller = s.Controller ?? string.Empty,
                    Action = s.ActName ?? s.Action ?? string.Empty,
                    DisplayName = $"{(string.IsNullOrEmpty(s.FrontNumber) ? "" : s.FrontNumber + ".")}{s.DisplayName ?? string.Empty}",
                    IconClass = s.IconClass ?? string.Empty,
                    Sort = s.Sort,
                    IsMenu = s.IsMenu,
                    Children = new List<MenuData>(),
                    IsActive = false,
                    Domain = "https://localhost:44372/",
                    IsBlank = s.IsBlank,
                    Enabled = s.Enabled,
                    Deleted = s.Deleted
                })
                .Where(w => w.IsMenu)
                .ToList();

            // 3️ 建立字典方便組樹
            var menuLookup = flatMenuList.ToDictionary(x => x.ID);
            var rootMenuList = new List<MenuData>();

            foreach (var item in flatMenuList)
            {
                if (item.ParentControllerMainID == Guid.Empty)
                {
                    rootMenuList.Add(item);
                }
                else if (menuLookup.TryGetValue(item.ParentControllerMainID, out var parent))
                {
                    parent.Children.Add(item);
                }
            }

            // 4️ 遞迴設定是否顯示 (有子節點才顯示)
            void SetIsShow(MenuData node)
            {
                if (node.Children != null && node.Children.Any())
                {
                    foreach (var child in node.Children)
                        SetIsShow(child);

                    // 只要有一個子節點 IsShow = true，父節點就顯示
                    node.IsMenu = node.Children.Any(c => c.IsMenu);
                }
                else
                {
                    node.IsMenu = true; // leaf 預設顯示
                }
            }

            // 5️ 遞迴所有根節點
            foreach (var root in rootMenuList)
                SetIsShow(root);

            // 6️ 設定目前選單的 Active 狀態
            currentController = (string)ViewContext.RouteData.Values["Controller"];
            currentAction = (string)ViewContext.RouteData.Values["Action"];
            // 用遞迴找出目前頁面對應的選單項目
            MenuData FindCurrentMenu(MenuData node)
            {
                if (node.Controller.Equals(currentController, StringComparison.OrdinalIgnoreCase) &&
                    node.Action.Equals(currentAction, StringComparison.OrdinalIgnoreCase) &&
                    node.IsMenu)
                {
                    return node;
                }

                foreach (var child in node.Children)
                {
                    var found = FindCurrentMenu(child);
                    if (found != null)
                        return found;
                }

                return null;
            }

            // 找出目前的 Menu 節點（從 rootMenuList 開始）
            MenuData currentMenuItem = null;
            foreach (var root in rootMenuList)
            {
                currentMenuItem = FindCurrentMenu(root);
                if (currentMenuItem != null)
                    break;
            }

            // 若找到，就一路往上標記 Active
            while (currentMenuItem != null)
            {
                currentMenuItem.IsActive = true;
                currentMenuItem = flatMenuList.FirstOrDefault(f => f.ID == currentMenuItem.ParentControllerMainID);
            }

            // 7️ 回傳樹狀 Menu
            result.List = rootMenuList;
            return View(result);
        }



        // 使用遞迴方式檢查 allMenus 中是否有有效子項目，若有效則要將父項目設為 true
        private bool HasValidChildItems(MenuData menu, List<MenuData> allMenus)
        {
            // 如果是末端節點且有 Controller 和 Action，則為有效選單Menu
            if (string.IsNullOrEmpty(menu.Controller) == false &&
                string.IsNullOrEmpty(menu.Action) == false)
            {
                return true;
            }

            // 當前menu取得找到父項目之下所有子項目
            var children = allMenus.Where(w => w.ParentControllerMainID == menu.ID).ToList();

            // 遞迴檢查每個子項目
            foreach (var child in children)
            {
                if (HasValidChildItems(child, allMenus))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
