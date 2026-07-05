namespace ERP.Web.Service.Options
{
    /// <summary>
    /// 英聽考卷 TTS（Azure Speech）設定；SubscriptionKey 留空時僅產生考卷結構，不呼叫 API。
    /// </summary>
    public class ExamTtsOptions
    {
        public const string SectionName = "ExamTts";

        public string Provider { get; set; } = "Azure";

        /// <summary>Azure Speech 訂閱金鑰（請勿 commit 至版控）</summary>
        public string SubscriptionKey { get; set; } = string.Empty;

        /// <summary>Azure 區域，例如 eastasia</summary>
        public string Region { get; set; } = "eastasia";

        /// <summary>題目為中文時使用的語音</summary>
        public string ChineseVoice { get; set; } = "zh-TW-HsiaoChenNeural";

        /// <summary>題目為英文時使用的語音</summary>
        public string EnglishVoice { get; set; } = "en-US-JennyNeural";

        /// <summary>MP3 快取目錄（可為相對 wwwroot 的路徑，啟動時會轉為絕對路徑）</summary>
        public string CacheDirectory { get; set; } = "exam-audio";

        /// <summary>對外 URL 前綴（對應 wwwroot 下的 CacheDirectory）</summary>
        public string PublicUrlPrefix { get; set; } = "/exam-audio";
    }
}
