using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;

namespace LifeTech.ERP.Utility.ViewComponents
{
    public class LeftSidebarViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;
        private ILogger _logger;

        public LeftSidebarViewComponent(IConfiguration configuration
            , ILogger<LeftSidebarViewComponent> logger)
        {
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = new NavigationViewModel();
            var currentDomain = $"{Request.Host}/";
            var currentProject = string.Empty;
            var currentController = (string)ViewContext.RouteData.Values["Controller"];
            var currentAction = (string)ViewContext.RouteData.Values["Action"];

            var userMenuKey = _configuration.GetSection("RedisSessionKey:UserMenu").Value;

            var userMenuMainSessionData = HttpContext.Session.GetString(userMenuKey);

            IEnumerable<dynamic> menuData = null;

            if (string.IsNullOrEmpty(userMenuMainSessionData) || userMenuMainSessionData == "[]")
            {
                menuData = await _controllerMainRepository.GetMenuDataAsync(HttpContext.User.Identity.Name);
                HttpContext.Session.SetString(userMenuKey, JsonConvert.SerializeObject(menuData));
            }
            else
            {
                menuData = JsonConvert.DeserializeObject<IEnumerable<ControllerMainTableModel>>(userMenuMainSessionData);
            }

            var menuList = new List<MenuData>();

            menuList = menuData?.Select(s => new MenuData
            {
                ID = s.ID.ToString(),
                ParentID = s.ControllerMainID.ToString(),
                PageNumber = s.PageNumber,
                ProjectName = s.StationCode,
                Level = s.Level,
                ControllerName = s.ControllerName,
                ActionName = s.ActName,
                Name = $"{s.FrontNumber}.{s.Name}",
                IconClass = s.IconClass,
                Sort = s.Sort,
                List = new List<MenuData>(),
                IsMenu = s.IsMenu,
                Domain = _sharedDomain[s.StationCode].ToString().Replace("http://", string.Empty).Replace("https://", string.Empty),
            }).ToList();


            var currentMenuItem = menuList.FirstOrDefault(w => w.Domain.ToLower() == currentDomain.ToLower() && w.ControllerName.ToLower() == currentController.ToLower() && w.ActionName.ToLower() == currentAction.ToLower() && w.IsMenu);
            while (currentMenuItem != null)
            {
                currentMenuItem.IsActive = true;
                currentMenuItem = menuList.FirstOrDefault(f => f.ID == currentMenuItem.ParentID);
            }

            menuList = menuList.Where(w => w.IsMenu).ToList();


            foreach (var item in menuList)
            {

                if (string.IsNullOrEmpty(item.ControllerName)
                    && string.IsNullOrEmpty(item.ActionName)
                    && !menuList.Any(a => a.ParentID == item.ID && !string.IsNullOrEmpty(a.ControllerName) && !string.IsNullOrEmpty(a.ActionName)))
                {
                    item.IsShow = false;
                }
                if (item.IsShow)
                {
                    menuList.Where(w => w.ID == item.ParentID && w.IsShow == false).ToList().ForEach(FE => { FE.IsShow = true; });
                }

            }

            menuList = menuList.Where(w => w.IsShow).ToList();


            var maxLevel = menuData.Where(w => w.IsMenu).Max(m => m.Level);

            for (int i = maxLevel ?? 0; i > 0; i--)
            {
                var levelGrupByParentID = menuList.Where(w => w.Level == i).GroupBy(g => g.ParentID);

                foreach (var item in levelGrupByParentID)
                {
                    var parentIndex = menuList.FindIndex(fi => fi.ID == item.Key);
                    if (parentIndex != -1)
                    {
                        menuList[parentIndex].List = item.Select(s => s).ToList();
                    }
                }
            }

            result.List = menuList.Where(s => s.Level == 1);

            foreach (PropertyInfo propertyInfo in _sharedDomain.GetType().GetProperties())
            {
                if (propertyInfo.Name != "Item")
                {
                    var domain = propertyInfo?.GetValue(_sharedDomain)?.ToString();
                    domain = domain?.Replace("http://", string.Empty);
                    domain = domain?.Replace("https://", string.Empty);

                    if (currentDomain == domain)
                    {
                        currentProject = propertyInfo.Name;
                    }
                }
            }

            var t1 = result.List;
            var t2 = t1.SelectMany(sm => sm.List);
            var t3 = t2.SelectMany(sm => sm.List);
            var t4 = t1.Concat(t2).Concat(t3);
            MenuData t5 = null;
            var currentItem = t4.Where(w => w.ProjectName.ToLower() == currentProject.ToLower() && w.ControllerName == currentController && w.ActionName == w.ActionName).FirstOrDefault();
            if (currentItem != null && currentItem.Level == 1)
            {
                t5 = currentItem;
            }
            else if (currentItem != null)
            {
                var currentParentID = currentItem.ParentID;
                t5 = t4.FirstOrDefault(f => f.ID == currentParentID);
                if (t5 != null && t5.Level != 1)
                {
                    t5 = t4.FirstOrDefault(f => f.ID == t5.ParentID);
                }
            }

            result.CurrentPageNumber = t5 == null ? 1 : t5.PageNumber;

            return View(result);
        }
    }
}
