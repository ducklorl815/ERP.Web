//using LifeTech.ERP.Utility.Models;
//using LifeTech.ERP.Utility.Models.TableModels;
//using LifeTech.ERP.Utility.ViewModels;
//using LifeTech.Utility.Helper;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using Org.BouncyCastle.Math.EC.Multiplier;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Threading.Tasks;

//namespace LifeTech.ERP.Utility.ViewComponents
//{
//    public class BreadcrumbViewComponent : ViewComponent
//    {
//        private readonly IConfiguration _configuration;
//        private readonly SharedDomain _sharedDomain;


//        public BreadcrumbViewComponent(
//            IConfiguration configuration
//            , IOptions<SharedDomain> options
//            )
//        {
//            _configuration = configuration;
//            _sharedDomain = options.Value;

//        }

//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            // 調整 3.1 .NET CORD 寫法改變
//            //var userMenuSessionData = HttpContext.Session.GetString(_configuration.GetSection("RedisSessionKey:UserMenu").Value);
//            var userMenuSessionData = HttpContext.Session.GetString(_configuration.GetSection("RedisSessionKey").GetSection("UserMenu").Value);

//            // 防呆 值=null抓不到 會有500 Error 
//            int i = 0;
//            while(string.IsNullOrWhiteSpace(userMenuSessionData) && i < 3)
//            {
//                i++;
//                userMenuSessionData = HttpContext.Session.GetString(_configuration.GetSection("RedisSessionKey").GetSection("UserMenu").Value);
//                //_logHelper.AddLog($"Request=>RedisSessionKey:{_configuration.GetSection("RedisSessionKey").GetSection("UserMenu").Value}");
//            }

//            var userMenuSessionDataObj = JsonConvert.DeserializeObject<IEnumerable<ControllerMainTableModel>>(userMenuSessionData ?? "");
//            var currentDomain = $"{Request.Host}/";
//            var currentProject = string.Empty;
//            var currentController = (string)ViewContext.RouteData.Values["Controller"];
//            var currentAction = (string)ViewContext.RouteData.Values["Action"];
//            var httpMethod = HttpContext.Request.Method.ToLower();
//            foreach (PropertyInfo propertyInfo in _sharedDomain.GetType().GetProperties())
//            {
//                if (propertyInfo.Name != "Item")
//                {
//                    var domain = propertyInfo?.GetValue(_sharedDomain)?.ToString();
//                    domain = domain?.Replace("http://", string.Empty);
//                    domain = domain?.Replace("https://", string.Empty);

//                    if (currentDomain == domain)
//                    {
//                        currentProject = propertyInfo.Name;
//                    }
//                }
//            }

//            var currentData = userMenuSessionDataObj?.FirstOrDefault(w => w.StationCode == currentProject && w.ControllerName == currentController && w.ActName == currentAction && w.HttpMethod.ToLower() == httpMethod);

//            List<ControllerData> list = new List<ControllerData>();

//            list.Add(new ControllerData
//            {
//                IsCurrent = true,
//                ControllerName = currentData?.ControllerName,
//                ActName = currentData?.ActName,
//                Name = currentData?.Name,
//                BreadcrumbType = currentData?.BreadcrumbType
//            });

//            var currentParentControllerID = currentData?.ControllerMainID;

//            while (currentParentControllerID != Guid.Empty)
//            {
//                var parentData = userMenuSessionDataObj?.FirstOrDefault(w => w.ID == currentParentControllerID);

//                if (parentData == null)
//                {
//                    currentParentControllerID = Guid.Empty;
//                }
//                else
//                {
//                    list.Insert(0, new ControllerData
//                    {
//                        IsCurrent = false,
//                        ControllerName = parentData.ControllerName,
//                        ActName = parentData.ActName,
//                        Name = parentData.Name,
//                        BreadcrumbType = parentData.BreadcrumbType
//                    });
//                    currentParentControllerID = parentData.ControllerMainID;
//                }
//            }

//            var title = currentData?.Name ?? "";
//            TempData["Title"] = title;

//            return View(new BreadcrumbViewComponentViewModel { Title = title, List = list });
//        }
//    }
//}
