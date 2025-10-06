namespace ERP.Web.Models.Models.ControllerSetting
{
    public class ControllerMainModel
    {
        public Guid ID { get; set; } // 假設有主鍵
        public string Controller { get; set; }
        public string DisplayName { get; set; }
        public string ControllerDesc { get; set; }
        public string HttpMethod { get; set; }
        public Guid ParentControllerMainID { get; set; }
        public string Action { get; set; }
        public Guid StationMainID { get; set; }
        public int PageNumber { get; set; }
        public int Level { get; set; }
        public int Sort { get; set; }
        public bool IsMenu { get; set; }
        public string FrontNumber { get; set; }
        public string IconClass { get; set; }
        public bool IsBlank { get; set; } = true;
        public string AbandonReason { get; set; }
    }
    public class ControllerListMainModel
    {
        public Guid ID { get; set; }
        public int Seq { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string DisplayName { get; set; }
        public string HttpMethod { get; set; }
        public int Level { get; set; }
        public int Sort { get; set; }
        public int ParentSeq { get; set; }
        public Guid ParentControllerMainID { get; set; }
        public int IsMenu { get; set; }
        public string ActionDesc { get; set; }
        public string Domain { get; set; }

    }
}
