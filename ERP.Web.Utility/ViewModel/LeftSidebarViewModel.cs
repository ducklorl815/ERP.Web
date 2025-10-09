namespace ERP.Web.Utility.ViewModel
{
    public class LeftSidebarViewModel
    {
        public List<MenuData> List { get; set; }
        public Guid CurrentNodeId { get; set; }
    }

    public class MenuData
    {
        public Guid ID { get; set; }
        public Guid ParentControllerMainID { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }       // 來自 ControllerAction
        public string ControllerActionID { get; set; }
        public string DisplayName { get; set; }
        public string ControllerDesc { get; set; }
        public string HttpMethod { get; set; }
        public Guid StationMainID { get; set; }
        public int Sort { get; set; }
        public bool IsMenu { get; set; }
        public string FrontNumber { get; set; }
        public string IconClass { get; set; }
        public bool IsBlank { get; set; }
        public string AbandonReason { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }

        public List<MenuData> Children { get; set; } = new();
        public bool IsActive { get; set; }
        public string Domain { get; set; }
    }
}
