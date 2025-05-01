using Microsoft.AspNetCore.Mvc;

namespace AddtoCart.Web.Controllers
{
    public class AddtoCartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddItem([FromBody] CartItemModel model)
        {
            // 這邊可以把資料加到Session或資料庫
            // 假設加到一個List<CartItemModel>裡

            // TODO: 儲存到購物車邏輯

            return Json(new { success = true });
        }

        public class CartItemModel
        {
            public string Type { get; set; }
            public string Frequency { get; set; }
            public DateTime StartDate { get; set; }
        }
        [HttpPost]
        public IActionResult AddHolidayDiscount([FromBody] HolidayDiscountModel model)
        {
            // TODO: 把折扣活動加入購物車資料結構
            return Json(new { success = true });
        }

        public class HolidayDiscountModel
        {
            public string Type { get; set; }
            public string Holiday { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }



    }
}
