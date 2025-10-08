using ERP.Web.Utility.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP.Web.Models.Models.ControllerSetting
{
    public class ErpMenuDataViewModel
    {
        public List<ErpMenuData> List { get; set; }
        public Guid CurrentNodeId { get; set; }
    }

    public class ErpMenuData
    {
        public Guid ID { get; set; }
        public Guid ControllerMainID { get; set; }
        public string ControllerName { get; set; }
        public string ActName { get; set; }       // 來自 ControllerAction
        public string ControllerActionID { get; set; }
        public string Name { get; set; }
        public string ControllerDesc { get; set; }
        public string HttpMethod { get; set; }
        public Guid StationMainID { get; set; }
        public int Level { get; set; }
        public int Sort { get; set; }
        public bool IsMenu { get; set; }
        public string FrontNumber { get; set; }
        public string IconClass { get; set; }
        public string AbandonReason { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }

        public List<ErpMenuData> Children { get; set; } = new();
        public bool IsActive { get; set; }
        public string Domain { get; set; }
    }
}
