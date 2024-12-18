using ERP.Web.Service.Service;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class ChartsController : Controller
    {
        private readonly ChartsService _chartsService;
        private readonly Guid SalerID;
        public ChartsController
            (
            ChartsService chartsService
            )
        {
            _chartsService = chartsService;
            SalerID = Guid.Parse("4DC64990-C818-4A28-AAEC-4C726F5E6CEB");
        }
        public async Task<IActionResult> Index()
        {
            var result = await _chartsService.GetChartsIndex(SalerID);
            return View(result);
        }

        public async Task<IActionResult> GetOrdersAmount()
        {
            var result = await _chartsService.GetOrdersAmount(SalerID);

            return Json(result);
        }
    }
}
