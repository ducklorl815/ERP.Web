namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>TTS 產音結果</summary>
    public class ExamTtsResult
    {
        public bool Success { get; set; }

        /// <summary>可供前端 audio src 使用的 URL，例如 /exam-audio/{file}.mp3</summary>
        public string? AudioUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public static ExamTtsResult Ok(string audioUrl) => new()
        {
            Success = true,
            AudioUrl = audioUrl
        };

        public static ExamTtsResult Fail(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };

        public static ExamTtsResult Skipped(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
