using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class SeatMapController : Controller
    {
        private readonly SeatMapService _seatMapService;
        public SeatMapController
            (
            SeatMapService seatMapService
            )
        {
            _seatMapService = seatMapService;
        }
        public async Task<IActionResult> Index()
        {
            SeatMapViewModel_result result = await _seatMapService.GetSeatMapList();
            return View(result);
        }

        public async Task<IActionResult> SaveSeatMap(string JosnString)
        {
            var XXX = await _seatMapService.GetSaveSeatMap(JosnString);
            return null;
        }
    }
}
