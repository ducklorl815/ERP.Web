namespace ERP.Web.Models.Models.Lession
{
    /// <summary>
    /// 訓練資訊主表 Model
    /// 對應資料表：[Lession].[dbo].[LessionInfo]
    /// </summary>
    public class EMTrainingInfo
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string URL { get; set; } = string.Empty;
        public int URLTime { get; set; }
        public string? Img { get; set; }
        public int Page { get; set; }
        /// <summary>
        /// 影片標籤/錨點 JSON 格式
        /// 格式：[{Time: int (秒數), Title: string (中文標題)}]
        /// </summary>
        public string? TagJson { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 影片標籤/錨點資訊 Model
    /// 用於解析 TagJson
    /// </summary>
    public class VideoTag
    {
        /// <summary>
        /// 時間點（秒數）
        /// </summary>
        public int Time { get; set; }
        
        /// <summary>
        /// 標籤標題（中文）
        /// </summary>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// 主要課程表 Model
    /// 對應資料表：[Lession].[dbo].[LessionMain]
    /// 用於管理主要課程及其段落資訊
    /// </summary>
    public class LessionMain
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        /// <summary>
        /// 課程名稱
        /// </summary>
        public string LessionName { get; set; } = string.Empty;
        /// <summary>
        /// 段落資訊 JSON 格式
        /// 格式：[{LessionInfoID: Guid, Paragraph: string}]
        /// </summary>
        public string? ParagraphJson { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 段落資訊 Model
    /// 用於解析 ParagraphJson
    /// </summary>
    public class ParagraphInfo
    {
        /// <summary>
        /// 訓練資訊 ID（對應 EMTrainingInfo.ID）
        /// </summary>
        public Guid LessionInfoID { get; set; }
        
        /// <summary>
        /// 段落描述
        /// </summary>
        public string Paragraph { get; set; } = string.Empty;
    }

    /// <summary>
    /// 訓練明細表 Model
    /// 對應資料表：[Lession].[dbo].[LessionTrainingDetl]
    /// 記錄員工觀看每個訓練影片的進度
    /// </summary>
    public class EMTrainingDetl
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public Guid EmployeeMainID { get; set; }
        public Guid LessionInfoID { get; set; }
        public int Time { get; set; }
        public bool State { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public Guid ModifyDept { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// 訓練總表 Model（已棄用）
    /// 對應資料表：EMTraining
    /// 記錄員工的整體訓練狀態
    /// 注意：此表已不再使用，練習狀態統計僅使用 EMTrainingDetl
    /// </summary>
    [Obsolete("此 Model 已不再使用，練習狀態統計僅使用 EMTrainingDetl")]
    public class EMTraining
    {
        public Guid ID { get; set; }
        public long Seq { get; set; }
        public Guid EmployeeMainID { get; set; }
        public int Time { get; set; }
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

