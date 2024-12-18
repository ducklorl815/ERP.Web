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
        public async Task<ChartsIndexViewModel_result> GetChartsIndex(Guid SalerID)
        {
            var result = new ChartsIndexViewModel_result();
            List<ChartsIndexMainModel> ChartsDataList = await _chartsRespo.GetChartsIndex(SalerID);
            result.YearAmount = ChartsDataList.Sum(x => x.OrderAmount);
            result.YearCount = ChartsDataList.Count();
            result.MonthAmount = ChartsDataList.Where(x => x.OrderDate.Month == DateTime.Now.Month).Sum(x => x.OrderAmount);
            result.MonthCount = ChartsDataList.Where(x => x.OrderDate.Month == DateTime.Now.Month).Count();
            result.DayAmount = ChartsDataList.Where(x => x.OrderDate == DateTime.Now.Date).Sum(x => x.OrderAmount);
            result.DayCount = ChartsDataList.Where(x => x.OrderDate == DateTime.Now.Date).Count();
            return result;
        }
        public async Task<OrdersAmountViewModel> GetOrdersAmount(Guid SalerID)
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
            List<OrdersAmountMainModel> OrdersAmountList = await _chartsRespo.GetOrdersAmount(targetYear, targetMonth, SalerID);

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

        public async Task<bool> InsertDailyData(int Count, DateTime OrderDate, int i)
        {
            if (Count <= 0)
                return true;
            List<Guid> SalerList = new List<Guid>();
            // 獲取業務員列表，並進行隨機排序
            SalerList = (await _chartsRespo.GetSalerList())
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            SalerList = SalerList.Take(Math.Max(0, SalerList.Count - 8)).ToList();

            foreach (var SalerID in SalerList)
            {
                Random randomTime = new Random();
                if (Count > 0)
                {
                    if (Count <= 0) break; // 避免過度扣減 Count
                    string tail = Count > 9 ? Count.ToString() : "0" + Count;

                    string OrderID = $"{DateTime.Now:yyMMddmmss}{tail}";

                    Random randomAmount = new Random();
                    int OrderAmount = await GetWeightedRandomAmount();
                    var ChartOrderModel = new ChartOrderMainModel
                    {
                        SalerID = SalerID,
                        OrderID = OrderID,
                        OrderAmount = OrderAmount,
                        ModifyUser = SalerID,
                        OrderDate = OrderDate,
                        ModifyDate = DateTime.Now.AddDays(-i).AddHours(randomTime.Next(4, 23)).AddMinutes(randomTime.Next(1, 59)).AddSeconds(randomTime.Next(1, 59)).AddMilliseconds(randomTime.Next(1, 999)),
                    };

                    await _chartsRespo.InsertDailyData(ChartOrderModel);
                    Count--;

                }
            }

            // 遞迴執行方法，直到 Count 小於等於 0
            return await InsertDailyData(Count, OrderDate, i);
        }
        public async Task<int> GetWeightedRandomAmount()
        {
            Random random = new Random();
            // 權重範圍設定
            var weightedRanges = new List<(int Min, int Max, int Weight)>
            {
                // 金額範圍,機率權重
                (4000, 12000, 2),
                (12001, 20000, 2),
                (20001, 30000, 6),
                (30001, 40000, 4),
                (40001, 60000, 2),
                (60001, 80000, 1),
                (80001, 100000, 1),
                (100001, 200000, 1),
            };

            // 計算權重總和
            int totalWeight = weightedRanges.Sum(r => r.Weight);

            // 生成隨機數，選擇區間
            int randomValue = random.Next(1, totalWeight + 1);
            int cumulativeWeight = 0;

            foreach (var range in weightedRanges)
            {
                cumulativeWeight += range.Weight;
                if (randomValue <= cumulativeWeight)
                {
                    // 在選中的區間內生成隨機數
                    return random.Next(range.Min, range.Max + 1);
                }
            }

            // 預設返回值（理論上不會到這裡）
            return 0;
        }


    }
}
