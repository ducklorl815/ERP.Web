namespace ERP.Web.Models.Models.ControllerSetting
{
    public class AccessGroupParallelModel
    {
        public string GroupName { get; set; }
        public List<NodeRecord> Nodes { get; set; }
    }
    public class NodeRecord
    {
        public Guid ID { get; set; }
        public string DisplayName { get; set; }
        public Guid ParentControllerMainID { get; set; }
    }

    public class AccessGroupModel
    {
        public Guid ID { get; set; }
        public string GroupName { get; set; }
        public string GroupDesc { get; set; }
        public string NodeJson { get; set; }
    }
}
