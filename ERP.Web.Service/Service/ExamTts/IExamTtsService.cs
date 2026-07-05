namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽考卷：文字轉語音（MP3）服務</summary>
    public interface IExamTtsService
    {
        /// <summary>Azure Key 是否已設定（未設定時不呼叫 API）</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// 取得或建立 MP3；快取檔名含 WordID 與文字 hash，文字變更時會重產。
        /// </summary>
        Task<ExamTtsResult> GetOrCreateMp3Async(
            Guid wordId,
            string speakText,
            string language,
            CancellationToken cancellationToken = default);
    }
}
