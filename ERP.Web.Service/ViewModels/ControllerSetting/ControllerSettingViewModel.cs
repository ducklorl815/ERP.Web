using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class ControllerSettingDataMaintainViewModel_result : ControllerSettingDataMaintainViewModel_param
    {
        public List<SelectListItem> StationListItem { get; set; }
        public List<SelectListItem> ControllerListItem { get; set; }
        public IconGroup IconGroup { get; set; }
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
        public string ID { get; set; }
    }
    public class ControllerSettingDataMaintainViewModel_param
    {
        public ControllerMainModel ControllerMain { get; set; }
    }

    public class IconGroup : IconGroup_param
    {
        public List<IconUtilityModel> IconList { get; set; }
    }

    public class IconGroup_param : PageViewModel
    {
        public IconKeyword IconKeyword { get; set; }
    }
    public class IconKeyword
    {
        public string IconClass { get; set; }
    }
}
