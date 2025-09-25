using ERP.Web.Utility.Models;
using ERP.Web.Utility.Respository;
using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = new LeftSidebarViewModel();
            var currentDomain = $"{Request.Host}/";
            var currentProject = string.Empty;
            var currentController = (string)ViewContext.RouteData.Values["Controller"];
            var currentAction = (string)ViewContext.RouteData.Values["Action"];

            var userMenuKey = _configuration.GetSection("RedisSessionKey:UserMenu").Value;

            IEnumerable<dynamic> menuData = null;
            try
            {
                menuData = await _controllerUtilityRepo.GetMenuDataAsync(HttpContext.User.Identity.Name);
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
                    ID = s.ID?.ToString() ?? Guid.NewGuid().ToString(),
                    ParentControllerMainID = s.ControllerMainID?.ToString() ?? "00000000-0000-0000-0000-000000000000",
                    Level = s.Level,
                    Controller = s.ControllerName ?? string.Empty,
                    ActionName = s.ActName ?? s.ActionName ?? string.Empty,
                    DisplayName = $"{s.FrontNumber ?? string.Empty}.{s.Name ?? s.DisplayName ?? string.Empty}",
                    IconClass = s.IconClass ?? string.Empty,
                    Sort = s.Sort,
                    IsMenu = s.IsMenu,
                    Children = new List<MenuData>(),
                    IsActive = false,
                    Domain = "",
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
                if (item.ParentControllerMainID == "00000000-0000-0000-0000-000000000000")
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

            // 6️ 回傳樹狀 Menu
            result.List = rootMenuList;
            return View(result);
        }



        // 使用遞迴方式檢查 allMenus 中是否有有效子項目，若有效則要將父項目設為 true
        private bool HasValidChildItems(MenuData menu, List<MenuData> allMenus)
        {
            // 如果是末端節點且有 Controller 和 Action，則為有效選單Menu
            if (string.IsNullOrEmpty(menu.Controller) == false &&
                string.IsNullOrEmpty(menu.ActionName) == false)
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
