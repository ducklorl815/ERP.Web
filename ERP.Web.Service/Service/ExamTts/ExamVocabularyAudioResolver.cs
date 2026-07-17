namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 英聽單字片段：優先從 Vocabulary.ExamAudio 或 seg_ 共用快取解析，避免重複 TTS。
    /// </summary>
    internal static class ExamVocabularyAudioResolver
    {
        /// <summary>
        /// 優先使用 DB 已記錄的 ExamAudio 路徑（須通過 .profile 內容比對，可跨考卷重用）。
        /// </summary>
        public static async Task<ExamTtsResult?> TryResolveStoredPathAsync(
            string? storedExamAudioPath,
            string segmentProfileKey,
            string publicUrlPrefix,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storedExamAudioPath) || !File.Exists(storedExamAudioPath))
                return null;

            if (await ExamTtsCacheHelper.IsSegmentCacheValidAsync(
                    storedExamAudioPath, segmentProfileKey, cancellationToken))
            {
                return ExamTtsResult.Ok(
                    ExamTtsCacheHelper.CombineUrl(publicUrlPrefix, Path.GetFileName(storedExamAudioPath)),
                    storedExamAudioPath);
            }

            return null;
        }

        /// <summary>
        /// 嘗試使用 seg_{hash}.mp3 共用快取（相同念法跨考卷共用，不依 seq_ 題號）。
        /// </summary>
        public static async Task<ExamTtsResult?> TryResolveSharedSegmentCacheAsync(
            string cacheDirectory,
            string publicUrlPrefix,
            string speakText,
            string language,
            string ttsProfileKey,
            CancellationToken cancellationToken = default)
        {
            var fileName = ExamTtsCacheHelper.BuildSegmentCacheFileName(speakText, language, ttsProfileKey);
            var filePath = ExamTtsCacheHelper.BuildPhysicalPath(cacheDirectory, fileName);
            if (!File.Exists(filePath))
                return null;

            var segmentProfileKey = ExamTtsCacheHelper.BuildSegmentProfileKey(speakText, language, ttsProfileKey);
            var profilePath = filePath + ".profile";

            // 舊版 seg_ 僅依 hash 檔名，無 .profile 時視為有效
            if (!File.Exists(profilePath)
                || await ExamTtsCacheHelper.IsSegmentCacheValidAsync(filePath, segmentProfileKey, cancellationToken))
            {
                return ExamTtsResult.Ok(
                    ExamTtsCacheHelper.CombineUrl(publicUrlPrefix, fileName),
                    filePath);
            }

            return null;
        }
    }
}
