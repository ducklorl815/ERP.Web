using ERP.Web.Service.Service;
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
            return View();
        }

        public async Task<IActionResult> SaveSeatMap(string JosnString)
        {
            var XXX = await _seatMapService.GetSaveSeatMap(JosnString);
            return null;
        }
    }
}
