using ERP.Web.Models;
using ERP.Web.Service.Service;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ERP.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController
            (
            ILogger<HomeController> logger,
            HomeService homeService
            )
        {
            _logger = logger;
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            bool update = await _homeService.InsertDailyData();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}