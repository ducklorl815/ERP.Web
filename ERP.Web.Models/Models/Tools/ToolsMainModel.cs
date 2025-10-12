using ERP.Web.Utility.Paging;

namespace ERP.Web.Models.Models.Tools
{
    public class FontAwesomeMainModel
    {
        public Guid ID { get; set; }
        public string IconStyle { get; set; }
        /// <summary>
        /// FontAwesome 的 class，例如 "fa-solid fa-house"
        /// </summary>
        public string IconClass { get; set; }
    }

    public class IconGroupMain_param : PageViewModel
    {
        public MainIconKeyword IconKeyword { get; set; }
    }
    public class MainIconKeyword
    {
        public string IconClass { get; set; }
    }
}
