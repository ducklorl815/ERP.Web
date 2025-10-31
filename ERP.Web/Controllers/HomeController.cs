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
            // 從 Session 取得登入使用者資料
            var userAccount = HttpContext.Session.GetString("UserAccount");
            var userName = HttpContext.Session.GetString("UserName");
            var userEmpID = HttpContext.Session.GetString("UserEmpID");
            var loginTime = HttpContext.Session.GetString("LoginTime");

            // 傳遞給 View 使用
            ViewData["UserAccount"] = userAccount ?? "未登入";
            ViewData["UserName"] = userName ?? "訪客";
            ViewData["UserEmpID"] = userEmpID ?? "N/A";
            ViewData["LoginTime"] = loginTime ?? "N/A";

            // 檢查是否已登入（可選）
            if (string.IsNullOrEmpty(userAccount))
            {
                // 未登入，可以選擇導向登入頁面
                // return RedirectToAction("Login", "Account");
            }

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