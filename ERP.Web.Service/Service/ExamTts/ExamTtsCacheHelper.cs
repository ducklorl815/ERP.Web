using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽 TTS 共用快取檔名與 URL 組合邏輯</summary>
    internal static class ExamTtsCacheHelper
    {
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

        /// <summary>整份考卷合併後的播放清單</summary>
        public static string BuildPlaylistCacheFileName(string playlistProfileKey)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(playlistProfileKey));
            var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            return $"playlist_{hash}.mp3";
        }

        public static string CombineUrl(string prefix, string fileName)
        {
            var basePath = (prefix ?? "/exam-audio").TrimEnd('/');
            return $"{basePath}/{fileName}";
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
            return Path.Combine(cacheDirectory, fileName);
        }
    }
}
