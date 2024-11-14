using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers
{
    public class SeatMapController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
