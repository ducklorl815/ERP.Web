namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽考卷每題獨立音檔結果</summary>
    public class ExamListeningTracksResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>依題號排序的每題完整音檔（含題號、重複念法與間隔）</summary>
        public List<ExamListeningTrackItem> Tracks { get; set; } = new();

        public static ExamListeningTracksResult Ok(IReadOnlyList<ExamListeningTrackItem> tracks) => new()
        {
            Success = true,
            Tracks = tracks.ToList()
        };

        public static ExamListeningTracksResult Fail(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };

        public static ExamListeningTracksResult Skipped(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }

    /// <summary>英聽考卷單題音檔資訊</summary>
    public class ExamListeningTrackItem
    {
        public int QuestionNumber { get; set; }

        public Guid WordId { get; set; }

        public string AudioUrl { get; set; } = string.Empty;

        public string DownloadName { get; set; } = string.Empty;

        public string? PhysicalPath { get; set; }
    }
}
