using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>英聽 TTS 共用快取檔名與 URL 組合邏輯</summary>
    internal static class ExamTtsCacheHelper
    {
        /// <summary>快取檔名：WordID + 語言 + 文字 hash，避免同字不同義時覆蓋錯誤。</summary>
        public static string BuildCacheFileName(Guid wordId, string speakText, string language)
        {
            var hashInput = $"{language}:{speakText.Trim()}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
            var hash = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
            return $"{wordId:N}_{hash}.mp3";
        }

        public static string CombineUrl(string prefix, string fileName)
        {
            var basePath = (prefix ?? "/exam-audio").TrimEnd('/');
            return $"{basePath}/{fileName}";
        }
    }
}
