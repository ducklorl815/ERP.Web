using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Paging;

namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class AccessGroupSearchListViewModel_result : AccessGroupSearchListViewModel_param
    {
        public List<AccessGroupModel> List { get; set; }
    }
    public class AccessGroupSearchListViewModel_param : PageViewModel
    {
        public AccessGroupKeyword Keyword { get; set; }
    }
    public class AccessGroupKeyword
    {
        public string GroupName { get; set; }
        public string GroupDesc { get; set; }
    }
    public class AccessGroupViewModel
    {
        public string ID { get; set; }
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
