//using LifeTech.ERP.Utility.Repository.LifeTourRepository;
//using LifeTech.ERP.Utility.ViewModels;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace LifeTech.ERP.Utility.ViewComponents
//{
//    public class InsideNavbarViewComponent : ViewComponent
//    {
//        private readonly MenuRepository _menuRepository;
//        public InsideNavbarViewComponent(IConfiguration configuration)
//        {
//            _menuRepository = new MenuRepository(configuration.GetConnectionString("UtilityLifeTour"));
//        }

//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            var result = new InsideNavbarViewModel();

//            result.List = await _menuRepository.GetMenuList();

//            return View(result);
//        }
//    }
//}
