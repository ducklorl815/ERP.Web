namespace ERP.Web.Service.ViewModels.ControllerSetting
{
    public class ControllerSettingActionListViewModel_result : ControllerSettingActionListViewModel_param
    {
        public List<ActionViewModel> ActionList { get; set; }

    }

    public class ActionViewModel
    {
        public Guid ActionID { get; set; }
        public string Controller { get; set; }
        public Guid ControllerID { get; set; }
        public string Action { get; set; }
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




}
