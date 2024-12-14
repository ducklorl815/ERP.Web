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
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetOrdersAmount()
        {
            var result = await _chartsService.GetOrdersAmount();

            return Json(result);
        }
    }
}
