namespace ERP.Web.Utility.Models
{
    public class ControllerMainTableModel
    {
        public int rn { get; set; }

        ///<summary></summary>
        public Guid ID { get; set; }

        ///<summary></summary>
        public long Seq { get; set; }

        ///<summary></summary>
        public string ControllerDesc { get; set; }

        ///<summary></summary>
        public string ControllerName { get; set; }

        ///<summary></summary>
        public string ActName { get; set; }

        ///<summary></summary>
        public string HttpMethod { get; set; }

        ///<summary></summary>
        public Guid ControllerMainID { get; set; }

        ///<summary></summary>
        public int Level { get; set; }

        ///<summary></summary>
        public int PageNumber { get; set; }

        ///<summary></summary>
        public string Name { get; set; }

        ///<summary></summary>
        public string IconClass { get; set; }

        ///<summary></summary>
        public string FrontNumber { get; set; }

        ///<summary></summary>
        public Guid StationMainID { get; set; }

        ///<summary></summary>
        public int Sort { get; set; }

        public string BreadcrumbType { get; set; }

        ///<summary></summary>
        public bool IsMenu { get; set; }

        ///<summary></summary>
        public DateTime CreateDate { get; set; }

        ///<summary></summary>
        public Guid CreateUser { get; set; }

        ///<summary></summary>
        public Guid CreateDept { get; set; }

        ///<summary></summary>
        public DateTime ModifyDate { get; set; }

        ///<summary></summary>
        public Guid ModifyUser { get; set; }

        ///<summary></summary>
        public Guid ModifyDept { get; set; }

        ///<summary></summary>
        public bool Enabled { get; set; }

        ///<summary></summary>
        public bool Deleted { get; set; }

        #region StationMain 
        public string StationCode { get; set; }

        public string StationName { get; set; }
        #endregion

        #region Other

        public long ParentSeq { get; set; }

        /// <summary>維護人員姓名</summary>
        public string ModifyEmpName { get; set; }

        /// <summary>維護人員部門</summary>
        public string ModifyDeptName { get; set; }

        public string ParentControllerName { get; set; }

        public string ParentActName { get; set; }

        public string ParentHttpMethod { get; set; }

        public string ParentName { get; set; }

        #endregion
    }
}
