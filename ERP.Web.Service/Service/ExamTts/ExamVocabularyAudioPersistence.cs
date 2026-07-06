using ERP.Web.Models.Respository;
using Microsoft.Extensions.Logging;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 英聽單字片段產生後，將實體路徑寫回 Vocabulary.ExamAudio。
    /// </summary>
    internal static class ExamVocabularyAudioPersistence
    {
        /// <summary>
        /// 若路徑與 DB 不同，執行 UPDATE Vocabulary SET ExamAudio = @ExamAudio WHERE ID = @ID
        /// </summary>
        public static async Task TrySaveAsync(
            ExamRespo examRepo,
            ILogger logger,
            Guid wordId,
            string? currentDbPath,
            string? resolvedPhysicalPath)
        {
            if (wordId == Guid.Empty || string.IsNullOrWhiteSpace(resolvedPhysicalPath))
                return;

            if (string.Equals(currentDbPath, resolvedPhysicalPath, StringComparison.OrdinalIgnoreCase))
                return;

            var updated = await examRepo.UpdateExamAudioAsync(wordId, resolvedPhysicalPath);
            if (!updated)
            {
                logger.LogWarning(
                    "寫入 Vocabulary.ExamAudio 失敗：WordId={WordId}，Path={Path}",
                    wordId,
                    resolvedPhysicalPath);
            }
        }
    }
}
