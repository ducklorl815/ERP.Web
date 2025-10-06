namespace ERP.Web.Service.ViewModels.ControllerSetting
{

    public class StationDataMaintainViewModel_result : StationDataMaintainViewModel_param
    {
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
    }
    public class StationDataMaintainViewModel_param
    {
        /// <summary>站台ID</summary>
        public string ID { get; set; }
        /// <summary>站台代碼</summary>
        public string StationCode { get; set; }

        /// <summary>站台名稱</summary>
        public string StationName { get; set; }

        /// <summary>域名</summary>
        public string Domain { get; set; }
        /// <summary>作廢</summary>
        public bool Abandon { get; set; }

        /// <summary>作廢原因</summary>
        public string AbandonReason { get; set; }
    }
}
