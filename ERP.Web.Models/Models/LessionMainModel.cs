namespace ERP.Web.Models.Models
{
    /// <summary>
    /// 訓練資訊主表 Model
    /// 對應資料表：EMTrainingInfo
    /// </summary>
    public class EMTrainingInfo
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public string MyModal { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public TimeSpan URLTime { get; set; }
        public string URLStyle { get; set; } = string.Empty;
        public string TitleStyle { get; set; } = string.Empty;
        public string ContentStyle { get; set; } = string.Empty;
        public string? Img { get; set; }
        public string ImgStyle { get; set; } = string.Empty;
        public int Page { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreateUser { get; set; }
        public Guid CreateDept { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 訓練明細表 Model
    /// 對應資料表：EMTrainingDetl
    /// 記錄員工觀看每個訓練影片的進度
    /// </summary>
    public class EMTrainingDetl
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public Guid EmployeeMainID { get; set; }
        public Guid EMTrainingInfoMainID { get; set; }
        public TimeSpan Time { get; set; }
        public bool State { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreateUser { get; set; }
        public Guid CreateDept { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 訓練總表 Model
    /// 對應資料表：EMTraining
    /// 記錄員工的整體訓練狀態
    /// </summary>
    public class EMTraining
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public Guid EmployeeMainID { get; set; }
        public TimeSpan Time { get; set; }
        public bool State { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid CreateUser { get; set; }
        public Guid CreateDept { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }
}

