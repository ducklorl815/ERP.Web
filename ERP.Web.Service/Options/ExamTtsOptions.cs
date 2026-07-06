namespace ERP.Web.Service.Options
{
    /// <summary>
    /// 英聽／中聽考卷 TTS 設定。Provider 可選 Azure 或 OpenAI；對應 ApiKey 留空時僅產生考卷結構，不呼叫 API。
    /// </summary>
    public class ExamTtsOptions
    {
        public const string SectionName = "ExamTts";

        /// <summary>TTS 供應商：Azure 或 OpenAI</summary>
        public string Provider { get; set; } = "OpenAI";

        /// <summary>Azure Speech 訂閱金鑰（請勿 commit 至版控）</summary>
        public string SubscriptionKey { get; set; } = string.Empty;

        /// <summary>Azure 區域，例如 eastasia</summary>
        public string Region { get; set; } = "eastasia";

        /// <summary>OpenAI API Key（請勿 commit 至版控）</summary>
        public string OpenAiApiKey { get; set; } = string.Empty;

        /// <summary>OpenAI TTS 模型：tts-1、tts-1-hd 或 gpt-4o-mini-tts</summary>
        public string OpenAiModel { get; set; } = "tts-1";

        /// <summary>題目為中文時使用的語音（Azure：zh-TW-HsiaoChenNeural；OpenAI：nova）</summary>
        public string ChineseVoice { get; set; } = "nova";

        /// <summary>題目為英文時使用的語音（Azure：en-US-JennyNeural；OpenAI：alloy）</summary>
        public string EnglishVoice { get; set; } = "alloy";

        /// <summary>MP3 快取目錄（可為相對 wwwroot 的路徑，啟動時會轉為絕對路徑）</summary>
        public string CacheDirectory { get; set; } = "exam-audio";

        /// <summary>對外 URL 前綴（對應 wwwroot 下的 CacheDirectory）</summary>
        public string PublicUrlPrefix { get; set; } = "/exam-audio";
    }
}
