using ERP.Web.Models.Models.ControllerSetting;

namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class ControllerSettingSearchListViewModel_result : ControllerSettingSearchListViewModel_param
    {
        public List<ControllerListMainModel> ControllerList { get; set; }
    }
    public class ControllerSettingSearchListViewModel_param
    {

    }

    public class ControllerSettingActionListViewModel_result : ControllerSettingActionListViewModel_param
    {
        public List<ActionViewModel> ActionList { get; set; }

    }

    public class ActionViewModel
    {
        public Guid ID { get; set; }
        public string Controller { get; set; }
        public string ActionName { get; set; }
        public string HttpMethod { get; set; }
        public string ActionDesc { get; set; }
        public string Enabled { get; set; }
        public string Deleted { get; set; }
    }
    public class ControllerSettingActionListViewModel_param
    {
        public string ControllerNameAndActName { get; set; }

        /// <summary>站台</summary>
        public string StationCode { get; set; }

        /// <summary>控制器</summary>
        public string ControllerName { get; set; }

        /// <summary>活動</summary>
        public string ActName { get; set; }

        /// <summary>方法</summary>
        public string HttpMethod { get; set; }

        /// <summary>是否作廢</summary>
        public string Abandon { get; set; }
    }
    public class ControllerSettingDataMaintainViewModel_result : ControllerSettingDataMaintainViewModel_param
    {
        public bool IsSuccess { get; set; }
        public string Msg { get; set; }
    }
    public class ControllerSettingDataMaintainViewModel_param
    {
        public Guid StationMainID { get; set; }
        public string DisplayName { get; set; }
        public string Controller { get; set; }
        public string ActionName { get; set; }
        public string HttpMethod { get; set; }
        public Guid ParentControllerMainID { get; set; }
        //public int PageNumber { get; set; }
        public string IconClass { get; set; }
        public string FrontNumber { get; set; }
        public int Sort { get; set; }
        public bool IsMenu { get; set; }
        public bool IsBlank { get; set; }
        public string ControllerDesc { get; set; }
        public int Level { get; set; }
        public int PageNumber { get; set; }
        
    }
}
