using ERP.Web.Utility.Paging;

namespace ERP.Web.Utility.Models
{
    public class IconModel : PageViewModel
    {
        public Guid ID { get; set; }

        /// <summary>
        /// FontAwesome 的 class，例如 "fa-solid fa-house"
        /// </summary>
        public string IconClass { get; set; } = string.Empty;

        /// <summary>
        /// Icon 的名稱，方便搜尋或顯示，例如 "house"
        /// </summary>
        public string IconName { get; set; } = string.Empty;
    }
}
