using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽 TTS 共用快取檔名與 URL 組合邏輯</summary>
    internal static class ExamTtsCacheHelper
    {
        /// <summary>題號播音固定檔名：label_01.mp3（第一題）～ label_99.mp3</summary>
        public static string BuildQuestionLabelFileName(int questionNumber)
        {
            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            return $"label_{questionNumber:D2}.mp3";
        }

        public static string BuildPhysicalPath(string cacheDirectory, string fileName) =>
            Path.Combine(cacheDirectory, fileName);

        /// <summary>快取檔名：WordID + 語言 + 文字 + TTS 設定 hash。</summary>
        public static string BuildCacheFileName(
            Guid wordId,
            string speakText,
            string language,
            string? ttsProfileKey = null)
        {
            var hashInput = string.IsNullOrWhiteSpace(ttsProfileKey)
                ? $"{language}:{speakText.Trim()}"
                : $"{language}:{speakText.Trim()}:{ttsProfileKey}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            return $"{wordId:N}_{hash}.mp3";
        }

        /// <summary>可重複使用的 TTS 片段（依文字＋語言＋設定 hash，不含 WordID）</summary>
        public static string BuildSegmentCacheFileName(
            string speakText,
            string language,
            string ttsProfileKey)
        {
            var hashInput = $"seg:{language}:{speakText.Trim()}:{ttsProfileKey}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            return $"seg_{hash}.mp3";
        }

        /// <summary>考卷單題聽力片段：seq_20260707_7_1.mp3（日期_次數_題號）</summary>
        public static string BuildExamSegmentFileName(DateTime examDate, int attemptNumber, int questionNumber)
        {
            if (attemptNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(attemptNumber), "考試次數須大於 0。");

            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            return $"seq_{examDate:yyyyMMdd}_{attemptNumber}_{questionNumber}.mp3";
        }

        /// <summary>seq_ 片段內容識別，用於 .profile 快取比對</summary>
        public static string BuildSegmentProfileKey(string speakText, string language, string ttsProfileKey) =>
            $"{language}:{speakText.Trim()}:{ttsProfileKey}";

        public static async Task<bool> IsSegmentCacheValidAsync(
            string filePath,
            string segmentProfileKey,
            CancellationToken cancellationToken = default)
        {
            var profilePath = filePath + ".profile";
            if (!File.Exists(filePath) || !File.Exists(profilePath))
                return false;

            var cachedProfile = await File.ReadAllTextAsync(profilePath, cancellationToken);
            return string.Equals(cachedProfile, segmentProfileKey, StringComparison.Ordinal);
        }

        public static Task WriteSegmentProfileAsync(
            string filePath,
            string segmentProfileKey,
            CancellationToken cancellationToken = default) =>
            File.WriteAllTextAsync(filePath + ".profile", segmentProfileKey, cancellationToken);

        /// <summary>
        /// 每題英聽完整音檔：{考卷名稱}_{題號}_{念出文字}.mp3
        /// 例：Fun Skills Unit42-43 SP 01_01_afternoon.mp3
        /// </summary>
        public static string BuildPerQuestionFileName(string examTitle, int questionNumber, string speakText)
        {
            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            var sanitizedTitle = SanitizeFileName(examTitle).Replace(".mp3", string.Empty, StringComparison.OrdinalIgnoreCase);
            var wordSuffix = SanitizeSpeakTextSuffix(speakText);
            return $"{sanitizedTitle}_{questionNumber:D2}_{wordSuffix}.mp3";
        }

        /// <summary>將 TTS 念出文字轉為檔名後綴（英文小寫、空白改底線）</summary>
        public static string SanitizeSpeakTextSuffix(string speakText)
        {
            if (string.IsNullOrWhiteSpace(speakText))
                return "word";

            var trimmed = speakText.Trim();
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(trimmed.Length);

            foreach (var c in trimmed)
            {
                if (Array.IndexOf(invalid, c) >= 0 || c == '.')
                    builder.Append('_');
                else if (char.IsWhiteSpace(c))
                    builder.Append('_');
                else
                    builder.Append(char.ToLowerInvariant(c));
            }

            var result = builder.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(result))
                return "word";

            // 避免檔名過長
            return result.Length <= 40 ? result : result[..40].TrimEnd('_');
        }

        /// <summary>整份考卷合併後的播放清單（與考卷 exam-title 一致的可讀檔名）</summary>
        public static string BuildPlaylistFileName(string displayName)
        {
            var sanitized = SanitizeFileName(displayName);
            return sanitized.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                ? sanitized
                : $"{sanitized}.mp3";
        }

        /// <summary>移除檔名不允許的字元，避免寫入快取目錄失敗</summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "exam-listening.mp3";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

            var result = builder.ToString().Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(result) ? "exam-listening.mp3" : result;
        }

        public static string CombineUrl(string prefix, string fileName)
        {
            var basePath = (prefix ?? "/exam-audio").TrimEnd('/');
            return $"{basePath}/{Uri.EscapeDataString(fileName)}";
        }

        /// <summary>由公開 URL 反查本機快取實體路徑</summary>
        public static string? ResolvePhysicalPath(string? publicUrl, string cacheDirectory, string publicUrlPrefix)
        {
            if (string.IsNullOrWhiteSpace(publicUrl))
                return null;

            var prefix = (publicUrlPrefix ?? "/exam-audio").TrimEnd('/');
            if (!publicUrl.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                return null;

            var fileName = publicUrl[(prefix.Length + 1)..];
            try
            {
                fileName = Uri.UnescapeDataString(fileName);
            }
            catch (UriFormatException)
            {
                // 保留原始檔名，相容舊版未編碼 URL
            }

            return Path.Combine(cacheDirectory, fileName);
        }
    }
}
