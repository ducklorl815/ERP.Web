using ERP.Web.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class ChartsController : Controller
    {
        private readonly ChartsService _chartsService;
        public ChartsController
            (
            ChartsService chartsService
            )
        {
            _chartsService = chartsService;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> GetOrdersAmount()
        {
            string SalerID = "4DC64990-C818-4A28-AAEC-4C726F5E6CEB";
            var result = await _chartsService.GetOrdersAmount(SalerID);

            return Json(result);
        }
    }
}
