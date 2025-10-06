using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class ControllerSettingCreateViewModel_result : ControllerSettingCreateViewModel_param
    {
        public List<SelectListItem> StationListItem { get; set; }
        public List<SelectListItem> ControllerListItem { get; set; }
        public IconGroup IconGroup { get; set; }
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
        public string ID { get; set; }
    }
    public class ControllerSettingCreateViewModel_param : PageViewModel
    {
        public ControllerMainModel ControllerMain { get; set; }

    }
}
