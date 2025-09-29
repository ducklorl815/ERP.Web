namespace ERP.Web.Utility.Models
{
    public class StationMainModel
    {
        /// <summary>站台ID</summary>
        public Guid ID { get; set; }

        /// <summary>站台代碼</summary>
        public string StationCode { get; set; }

        /// <summary>站台名稱</summary>
        public string StationName { get; set; }

        /// <summary>域名</summary>
        public string Domain { get; set; }


        /// <summary>控制器名稱</summary>
        public string Controller { get; set; }
        /// <summary>動作名稱</summary>
        public string ActionName { get; set; }

        /// <summary>顯示名稱</summary>
        public string DisplayName { get; set; }

        /// <summary>使用方法</summary>
        public string HttpMethod { get; set; }
    }
}
