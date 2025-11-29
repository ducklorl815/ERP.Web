using LifeTech.ERP.Emanage.Web.Models.Models.Lession;
using LifeTech.ERP.Emanage.Web.Service.Service.Lession;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeTech.ERP.Emanage.Web.Service.ViewModels
{
    /// <summary>
    /// 課程列表 ViewModel
    /// </summary>
    public class LessionIndexViewModel
    {
        public List<LessionMainProgress> LessionMainProgressList { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 課程播放 ViewModel
    /// </summary>
    public class LessionPlayViewModel
    {
        public EMTrainingInfo TrainingInfo { get; set; } = null!;
        public Guid EmployeeMainID { get; set; }
        public int WatchedTime { get; set; } // 已觀看時間（秒）
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 更新進度請求
    /// </summary>
    public class UpdateProgressRequest
    {
        public string LessionInfoID { get; set; } = string.Empty;
        public int WatchedTime { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// 多片段課程播放 ViewModel
    /// </summary>
    public class LessionMultiSegmentViewModel
    {
        public Guid LessionMainID { get; set; }
        public string LessionMainName { get; set; } = string.Empty;
        public List<EMTrainingInfo> Segments { get; set; } = new();
        public Dictionary<Guid, EMTrainingDetl?> ProgressDict { get; set; } = new();
        public Guid EmployeeMainID { get; set; }
    }

    /// <summary>
    /// 新增課程 ViewModel
    /// </summary>
    public class LessionCreateViewModel
    {
        [Display(Name = "選擇課程")]
        public Guid? LessionMainID { get; set; }

        [Display(Name = "建立新課程")]
        public bool IsNewLessionMain { get; set; }

        [Display(Name = "新課程名稱")]
        public string? NewLessionMainName { get; set; }

        public List<SelectListItem> LessionMainList { get; set; } = new();

        [Required(ErrorMessage = "標題為必填")]
        [Display(Name = "片段標題")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "內容描述")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "YouTube URL 為必填")]
        [Display(Name = "YouTube 影片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string URL { get; set; } = string.Empty;

        [Required(ErrorMessage = "影片時長為必填")]
        [Display(Name = "影片時長 (格式: HH:MM:SS 或 MM:SS)")]
        public string URLTimeString { get; set; } = string.Empty;

        [Display(Name = "圖片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string? Img { get; set; }

        [Display(Name = "影片標籤/錨點")]
        public string? TagJson { get; set; }
    }

    /// <summary>
    /// 修改課程 ViewModel
    /// </summary>
    public class LessionEditViewModel
    {
        public Guid ID { get; set; }

        [Display(Name = "選擇課程")]
        [Required(ErrorMessage = "請選擇課程")]
        public Guid? LessionMainID { get; set; }

        public List<SelectListItem> LessionMainList { get; set; } = new();

        [Required(ErrorMessage = "標題為必填")]
        [Display(Name = "片段標題")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "內容描述")]
        public string? Content { get; set; }

        [Required(ErrorMessage = "YouTube URL 為必填")]
        [Display(Name = "YouTube 影片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string URL { get; set; } = string.Empty;

        [Required(ErrorMessage = "影片時長為必填")]
        [Display(Name = "影片時長 (格式: HH:MM:SS 或 MM:SS)")]
        public string URLTimeString { get; set; } = string.Empty;

        [Display(Name = "圖片網址")]
        [Url(ErrorMessage = "請輸入有效的 URL")]
        public string? Img { get; set; }

        [Display(Name = "影片標籤/錨點")]
        public string? TagJson { get; set; }
    }

    /// <summary>
    /// 取得 YouTube 影片資訊請求
    /// </summary>
    public class GetYouTubeVideoInfoRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 課程管理列表 ViewModel
    /// </summary>
    public class LessionSearchListViewModel
    {
        public List<LessionMainWithSegmentCount> LessionListWithSegmentCount { get; set; } = new();
    }


    /// <summary>
    /// 課程詳細資訊 ViewModel
    /// </summary>
    public class LessionDetailsViewModel
    {
        public LessionMain LessionMain { get; set; } = null!;
        public List<EMTrainingInfo> Segments { get; set; } = new();
    }

    /// <summary>
    /// YouTube API 響應模型
    /// </summary>
    public class YouTubeApiResponse
    {
        public List<YouTubeVideoItem> Items { get; set; } = new();
    }

    /// <summary>
    /// YouTube 影片項目
    /// </summary>
    public class YouTubeVideoItem
    {
        public YouTubeContentDetails? ContentDetails { get; set; }
    }

    /// <summary>
    /// YouTube 內容詳情
    /// </summary>
    public class YouTubeContentDetails
    {
        public string? Duration { get; set; }
    }
}
