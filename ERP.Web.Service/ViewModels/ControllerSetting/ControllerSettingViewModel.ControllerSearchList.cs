using ERP.Web.Models.Models.ControllerSetting;

namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class ControllerSettingSearchListViewModel_result : ControllerSettingSearchListViewModel_param
    {
        public List<ControllerListMainModel> ControllerList { get; set; }
    }
    public class ControllerSettingSearchListViewModel_param
    {
        public Keyword Keyword { get; set; }
    }
    public class Keyword
    {
        public string StationCode { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string DisplayName { get; set; }
        public string Domain { get; set; }
    }
}
