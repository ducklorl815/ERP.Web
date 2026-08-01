using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽 TTS 共用快取檔名與 URL 組合邏輯</summary>
    internal static class ExamTtsCacheHelper
    {
        /// <summary>跨考卷共用音檔資料夾（題號播音、seg_ 單字片段等）</summary>
        public const string PublicFolderName = "Public";

        /// <summary>題號播音固定檔名：label_01.mp3（第一題）～ label_99.mp3</summary>
        public static string BuildQuestionLabelFileName(int questionNumber)
        {
            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            return $"label_{questionNumber:D2}.mp3";
        }

        /// <summary>考卷資料夾：{yyyyMMdd}_{title}，例：20260723_Fun Skills Unit52-53 SP 01</summary>
        public static string BuildExamFolderName(DateTime examDate, string examTitle)
        {
            var sanitizedTitle = SanitizeFolderName(examTitle);
            return $"{examDate:yyyyMMdd}_{sanitizedTitle}";
        }

        /// <summary>
        /// 考卷資料夾內每題音檔：僅題號，例：01.mp3、02.mp3。
        /// 播放器依檔名排序即可依題序播放；單字細節放在 .profile。
        /// </summary>
        public static string BuildPerQuestionFileName(int questionNumber)
        {
            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            return $"{questionNumber:D2}.mp3";
        }

        /// <summary>
        /// 每題音檔對應的 profile 檔名：{題號}_{單字}.mp3.profile
        /// 例：01_jacket.mp3.profile（與 01.mp3 成對）
        /// </summary>
        public static string BuildPerQuestionProfileFileName(int questionNumber, string speakText)
        {
            if (questionNumber < 1 || questionNumber > 99)
                throw new ArgumentOutOfRangeException(nameof(questionNumber), "題號僅支援 1～99。");

            var wordSuffix = SanitizeSpeakTextSuffix(speakText);
            return $"{questionNumber:D2}_{wordSuffix}.mp3.profile";
        }

        /// <summary>考卷音檔相對路徑：{日期_title}/01.mp3</summary>
        public static string BuildPerQuestionRelativePath(
            DateTime examDate,
            string examTitle,
            int questionNumber)
        {
            var folder = BuildExamFolderName(examDate, examTitle);
            var fileName = BuildPerQuestionFileName(questionNumber);
            return Path.Combine(folder, fileName);
        }

        /// <summary>共用檔相對路徑：Public/{fileName}</summary>
        public static string BuildPublicRelativePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("檔名不可為空。", nameof(fileName));

            var trimmed = fileName.Replace('\\', '/').Trim().TrimStart('/');
            if (trimmed.StartsWith(PublicFolderName + "/", StringComparison.OrdinalIgnoreCase))
                return trimmed.Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(PublicFolderName, Path.GetFileName(trimmed));
        }

        public static string GetPublicDirectory(string cacheDirectory) =>
            Path.Combine(cacheDirectory, PublicFolderName);

        public static string BuildPhysicalPath(string cacheDirectory, string relativePath) =>
            Path.Combine(cacheDirectory, relativePath);

        /// <summary>
        /// 解析共用快取實體路徑：優先 Public/，其次根目錄（相容舊版扁平結構）。
        /// </summary>
        public static bool TryResolvePublicOrLegacyPath(
            string cacheDirectory,
            string fileName,
            out string physicalPath,
            out string relativePath)
        {
            var publicRelative = BuildPublicRelativePath(fileName);
            var publicPath = BuildPhysicalPath(cacheDirectory, publicRelative);
            if (File.Exists(publicPath))
            {
                physicalPath = publicPath;
                relativePath = publicRelative;
                return true;
            }

            var legacyPath = BuildPhysicalPath(cacheDirectory, fileName);
            if (File.Exists(legacyPath))
            {
                physicalPath = legacyPath;
                relativePath = fileName;
                return true;
            }

            // 尚未存在：預設寫入 Public
            physicalPath = publicPath;
            relativePath = publicRelative;
            return false;
        }

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

        /// <summary>資料夾名稱清理（不可含路徑分隔字元）</summary>
        public static string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return "exam-listening";

            var sanitized = SanitizeFileName(folderName)
                .Replace(".mp3", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim()
                .TrimEnd('.');

            return string.IsNullOrWhiteSpace(sanitized) ? "exam-listening" : sanitized;
        }

        /// <summary>
        /// 組合公開 URL；relativePath 可含子資料夾（例：Public/label_01.mp3、20260723_Title/01_jacket.mp3）。
        /// 各路徑區段分別編碼，保留 '/' 分隔。
        /// </summary>
        public static string CombineUrl(string prefix, string relativePath)
        {
            var basePath = (prefix ?? "/exam-audio").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(relativePath))
                return basePath;

            var normalized = relativePath.Replace('\\', '/').Trim().TrimStart('/');
            var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var encoded = string.Join('/', parts.Select(Uri.EscapeDataString));
            return $"{basePath}/{encoded}";
        }

        /// <summary>由快取根目錄下的實體路徑轉成相對路徑（供組 URL）</summary>
        public static string GetRelativePathUnderCache(string physicalPath, string cacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(physicalPath))
                return string.Empty;

            try
            {
                var fullCache = Path.GetFullPath(cacheDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var fullFile = Path.GetFullPath(physicalPath);
                var cachePrefix = fullCache + Path.DirectorySeparatorChar;

                if (fullFile.StartsWith(cachePrefix, StringComparison.OrdinalIgnoreCase))
                    return Path.GetRelativePath(fullCache, fullFile);

                if (string.Equals(fullFile, fullCache, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;
            }
            catch (Exception)
            {
                // 路徑異常時退回檔名
            }

            return Path.GetFileName(physicalPath);
        }

        /// <summary>由公開 URL 反查本機快取實體路徑</summary>
        public static string? ResolvePhysicalPath(string? publicUrl, string cacheDirectory, string publicUrlPrefix)
        {
            if (string.IsNullOrWhiteSpace(publicUrl))
                return null;

            var prefix = (publicUrlPrefix ?? "/exam-audio").TrimEnd('/');
            if (!publicUrl.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                return null;

            var relative = publicUrl[(prefix.Length + 1)..];
            try
            {
                // 區段可能各自編碼，整段 Unescape 後再組路徑
                relative = string.Join(
                    Path.DirectorySeparatorChar,
                    relative.Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .Select(Uri.UnescapeDataString));
            }
            catch (UriFormatException)
            {
                relative = relative.Replace('/', Path.DirectorySeparatorChar);
            }

            return Path.Combine(cacheDirectory, relative);
        }
    }
}
