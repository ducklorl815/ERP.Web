using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using ERP.Web.Service.ViewModels;

namespace ERP.Web.Service.Service
{
    public class ChartsService
    {
        private readonly ChartsRespo _chartsRespo;
        public ChartsService
            (
            ChartsRespo chartsRespo
            )
        {
            _chartsRespo = chartsRespo;
        }
        public async Task<OrdersAmountViewModel> GetOrdersAmount()
        {
            var result = new OrdersAmountViewModel
            {
                IsSuccess = false,
                TotalAmount = new List<List<int>>(),
                TopAmount = new List<List<int>>(),
                OrderCount = new List<List<int>>(),
                OrderDate = new List<List<string>>(),
            };
            var targetYear = DateTime.Now.Year;
            var targetMonth = DateTime.Now.Month;
            List<OrdersAmountMainModel> OrdersAmountList = await _chartsRespo.GetOrdersAmount(targetYear, targetMonth);

            if (OrdersAmountList == null)
                return result;

            // 將資料轉換為 Amount 和 Count 格式
            result.TotalAmount = OrdersAmountList.Select((x, index) => new List<int> { index, x.TotalAmount }).ToList();
            result.TopAmount = OrdersAmountList.Select((x, index) => new List<int> { index, x.TopAmount }).ToList();
            result.OrderCount = OrdersAmountList.Select((x, index) => new List<int> { index, x.OrderCount }).ToList();
            result.OrderDate = OrdersAmountList.Select((x, index) => new List<string> { index.ToString(), x.OrderDate.ToString("dd") }).ToList();

            result.IsSuccess = true;
            return result;
        }
    }
}
