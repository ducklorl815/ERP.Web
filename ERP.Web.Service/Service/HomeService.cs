using ERP.Web.Models.Respository;

namespace ERP.Web.Service.Service
{
    public class HomeService
    {
        private readonly ChartsRespo _chartsRespo;
        private readonly ChartsService _chartsService;
        public HomeService
            (
            ChartsRespo chartsRespo,
            ChartsService chartsService
            )
        {
            _chartsRespo = chartsRespo;
            _chartsService = chartsService;
        }
        public async Task<bool> InsertDailyData()
        {
            bool InsertDaily = false;
            int Days = 7;
            for (int i = 0; i < Days; i++)
            {
                Random random = new Random();
                int Count = random.Next(2, 18);
                DateTime OrderDate = DateTime.Now.AddDays(-i);
                if (!await _chartsRespo.chkExistDaily(OrderDate))
                    InsertDaily = await _chartsService.InsertDailyData(Count, OrderDate, i);
            }

            return InsertDaily;
        }
    }
}
