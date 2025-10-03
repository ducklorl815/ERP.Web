using ERP.Web.Utility.Paging;

namespace ERP.Web.Utility.Models
{
    public class IconUtilityModel
    {
        public Guid ID { get; set; }
        public string IconStyle { get; set; }
        /// <summary>
        /// FontAwesome 的 class，例如 "fa-solid fa-house"
        /// </summary>
        public string IconClass { get; set; }

        public string IconName { get; set; }
    }
}
