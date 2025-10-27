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
        /// <summary>
        /// 顯示座位圖主頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SeatMapViewModel_result result = await _seatMapService.GetSeatMapList();
            return View(result);
        }

        /// <summary>
        /// 儲存座位圖資料
        /// </summary>
        /// <param name="JosnString">座位資料的 JSON 字串</param>
        [HttpPost]
        public async Task<IActionResult> SaveSeatMap(string JosnString)
        {
            try
            {
                var result = await _seatMapService.GetSaveSeatMap(JosnString);
                return Ok(new 
                { 
                    success = true, 
                    message = "儲存成功",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = $"儲存失敗：{ex.Message}" 
                });
            }
        }
    }
}
