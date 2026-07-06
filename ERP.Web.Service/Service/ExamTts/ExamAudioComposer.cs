using NAudio.MediaFoundation;
using NAudio.Wave;

namespace ERP.Web.Service.Service.ExamTts
{
    /// <summary>
    /// 將多個 MP3 片段與靜音間隔合併為單一 MP3（Windows 使用 Media Foundation 編碼）。
    /// </summary>
    internal static class ExamAudioComposer
    {
        private const int TargetSampleRate = 24000;
        private const int TargetChannels = 1;
        private const int TargetBitsPerSample = 16;

        private static readonly object MediaFoundationLock = new();
        private static bool _mediaFoundationStarted;

        public static void Compose(
            IReadOnlyList<string> mp3FilePaths,
            IReadOnlyList<double> silenceAfterSeconds,
            string outputMp3Path)
        {
            if (mp3FilePaths.Count == 0)
                throw new InvalidOperationException("沒有可合併的音檔片段。");

            EnsureMediaFoundation();

            var targetFormat = new WaveFormat(TargetSampleRate, TargetBitsPerSample, TargetChannels);
            var tempWavPath = outputMp3Path + ".tmp.wav";

            try
            {
                using (var writer = new WaveFileWriter(tempWavPath, targetFormat))
                {
                    for (var i = 0; i < mp3FilePaths.Count; i++)
                    {
                        AppendMp3Segment(writer, mp3FilePaths[i], targetFormat);

                        if (i < silenceAfterSeconds.Count)
                        {
                            var silenceSeconds = silenceAfterSeconds[i];
                            if (silenceSeconds > 0)
                                AppendSilence(writer, targetFormat, silenceSeconds);
                        }
                    }
                }

                using var wavReader = new WaveFileReader(tempWavPath);
                MediaFoundationEncoder.EncodeToMp3(wavReader, outputMp3Path, 128000);
            }
            finally
            {
                if (File.Exists(tempWavPath))
                    File.Delete(tempWavPath);
            }
        }

        private static void EnsureMediaFoundation()
        {
            if (_mediaFoundationStarted)
                return;

            lock (MediaFoundationLock)
            {
                if (_mediaFoundationStarted)
                    return;

                MediaFoundationApi.Startup();
                _mediaFoundationStarted = true;
            }
        }

        private static void AppendMp3Segment(WaveFileWriter writer, string mp3Path, WaveFormat targetFormat)
        {
            using var reader = new Mp3FileReader(mp3Path);
            using var resampler = new MediaFoundationResampler(reader, targetFormat)
            {
                ResamplerQuality = 60
            };

            var buffer = new byte[targetFormat.AverageBytesPerSecond];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                writer.Write(buffer, 0, bytesRead);
        }

        private static void AppendSilence(WaveFileWriter writer, WaveFormat format, double seconds)
        {
            var totalBytes = (int)Math.Round(format.AverageBytesPerSecond * seconds);
            var chunk = new byte[Math.Min(totalBytes, format.AverageBytesPerSecond)];
            var remaining = totalBytes;

            while (remaining > 0)
            {
                var writeSize = Math.Min(remaining, chunk.Length);
                writer.Write(chunk, 0, writeSize);
                remaining -= writeSize;
            }
        }
    }
}
