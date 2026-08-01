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

        /// <summary>
        /// 語速（OpenAI：0.25～4.0，1.0 為預設；建議英聽 0.75～0.9 略慢）。
        /// Azure 亦會套用於 SSML prosody rate。
        /// </summary>
        public double Speed { get; set; } = 0.85;

        /// <summary>中文題語速；未設定時使用 Speed</summary>
        public double? ChineseSpeed { get; set; }

        /// <summary>英文題語速；未設定時使用 Speed</summary>
        public double? EnglishSpeed { get; set; }

        /// <summary>
        /// OpenAI gpt-4o-mini-tts 中文口音／語氣指示（僅支援 instructions 的模型有效）。
        /// 建議使用台灣國語描述，避免大陸腔。
        /// </summary>
        public string OpenAiChineseInstructions { get; set; } =
            "Speak in Taiwan Mandarin (台灣國語). Tone: clear and friendly. Pace: slow and steady, easy for students to follow in a listening test.";

        /// <summary>OpenAI gpt-4o-mini-tts 英文語氣指示（選填）</summary>
        public string? OpenAiEnglishInstructions { get; set; }

        /// <summary>念完「第 N 題」後的靜音秒數</summary>
        public int PauseAfterQuestionLabelSeconds { get; set; } = 3;

        /// <summary>念完題目單字後的靜音秒數</summary>
        public int PauseAfterWordSeconds { get; set; } = 3;

        /// <summary>每題結束（第二遍單字後）到下一題的靜音秒數</summary>
        public int PauseAfterQuestionBlockSeconds { get; set; } = 10;

        /// <summary>
        /// MP3 實體快取目錄（建議 AppData/exam-audio，勿放 wwwroot 以免建置清除）。
        /// 結構：Public/（共用片段）、{yyyyMMdd}_{title}/（每題完整音檔）。
        /// </summary>
        public string CacheDirectory { get; set; } = "AppData/exam-audio";

        /// <summary>對外 URL 前綴（對應 CacheDirectory）</summary>
        public string PublicUrlPrefix { get; set; } = "/exam-audio";
    }
}
