namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class AccessGroupViewModel
    {
        public string GroupName { get; set; }
        public string GroupDesc { get; set; }
        public List<TreeNodeModel> SelectedNodes { get; set; }
    }

    public class AccessGroupViewModel_param
    {
        public List<string> ModuleIDs { get; set; }
    }
    public class TreeNodeModel
    {
        public Guid ID { get; set; }      // 對應 JSON id
        public string DisplayName { get; set; }  // 對應 JSON text
        public List<TreeNodeModel> Children { get; set; } // 對應 JSON children
    }
}
