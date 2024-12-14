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
                Amount = new List<List<int>>(), // 修正型別
                Count = new List<List<int>>()   // 修正型別
            };
            List<OrdersAmountMainModel> OrdersAmountList = await _chartsRespo.GetOrdersAmount();

            if (OrdersAmountList == null)
                return result;

            // 將資料轉換為 Amount 和 Count 格式
            result.Amount = OrdersAmountList.Select((x, index) => new List<int> { index, x.Amount }).ToList();
            result.Count = OrdersAmountList.Select((x, index) => new List<int> { index, x.Count }).ToList();

            result.IsSuccess = true;
            return result;
        }
    }
}
