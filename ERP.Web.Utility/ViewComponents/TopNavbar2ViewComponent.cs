//using LifeTech.ERP.Utility.Models.TableModels;
//using LifeTech.ERP.Utility.Repository.LifeTourRepository;
//using LifeTech.ERP.Utility.ViewModels;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LifeTech.ERP.Utility.ViewComponents
//{
//    public class TopNavbar2ViewComponent : ViewComponent
//    {
//        private readonly MenuRepository _menuRepository;
//        public TopNavbar2ViewComponent(IConfiguration configuration)
//        {
//            _menuRepository = new MenuRepository(configuration.GetConnectionString("UtilityLifeTour"));
//        }

//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            var result = new TopNavbar2ViewModel();

//            List<EnvironmentSettingModel> SettingList = await _menuRepository.GetViewMenu();
//            if (SettingList.Where(x => x.Name == "TopNavbar2" && x.Enabled == true).Any())
//            {
//                result.List = await _menuRepository.GetMenuList();
//            }

//            return View(result);
//        }
//    }
//}
