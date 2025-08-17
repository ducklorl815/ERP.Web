using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using ERP.Web.Service.ViewModels;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service
{
    public class SeatMapService
    {
        private readonly SeatMapRespo _seatMapRespo;
        private readonly Guid ModifyUser;
        public SeatMapService
            (
            SeatMapRespo seatMapRespo
            )
        {
            _seatMapRespo = seatMapRespo;
            ModifyUser = Guid.Parse("4DC64990-C818-4A28-AAEC-4C726F5E6CEB");
        }
        public async Task<bool> GetSaveSeatMap(string josnString)
        {
            var SeatMapData = new SeatMapMainModel();
            try
            {
                SeatMapData = JsonConvert.DeserializeObject<SeatMapMainModel>(josnString);

            }
            catch (Exception)
            {

                throw;
            }
            SeatMapData.ModifyUser = ModifyUser;
            SeatMapData.ModifyDate = DateTime.Now;
            Guid ExistID = await _seatMapRespo.ChkExistSeatMap(SeatMapData);
            SeatMapData.ID = ExistID;
            bool chkSuccess = ExistID == Guid.Empty ? await _seatMapRespo.GetInsertSeatMap(SeatMapData) : await _seatMapRespo.GetUpdateSeatMap(SeatMapData);
            return chkSuccess;
        }

        public async Task<SeatMapViewModel_result> GetSeatMapList()
        {
            var result = new SeatMapViewModel_result();

            result.SeatMap = await _seatMapRespo.GetSeatMapList();
            return result;
        }
    }
}
