using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;

namespace ERP.Web.Service.ViewModels.Tools
{
    public class FontAwesomeViewModel_result
    {
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
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
