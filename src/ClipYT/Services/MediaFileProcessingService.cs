using ClipYT.Enums;
using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class MediaFileProcessingService : IMediaFileProcessingService
    {
        private readonly string _youtubeDlpPath;
        private readonly string _ffmpegPath;
        private readonly string _outputFolder;
        private readonly string _previewCacheFolder;
        private readonly IHubContext<ProgressHub> _hubContext;

        public MediaFileProcessingService(IConfiguration configuration, IHubContext<ProgressHub> hubContext)
        {
            _ffmpegPath = configuration["Config:FFmpegPath"] ?? throw new ArgumentNullException(nameof(configuration), "Config:FFmpegPath is missing");
            _youtubeDlpPath = configuration["Config:YoutubeDlpPath"] ?? throw new ArgumentNullException(nameof(configuration), "Config:YoutubeDlpPath is missing");
            _outputFolder = configuration["Config:OutputFolder"] ?? throw new ArgumentNullException(nameof(configuration), "Config:OutputFolder is missing");
            _previewCacheFolder = Path.Combine(_outputFolder, "preview-cache");
            _hubContext = hubContext;

            if (!Directory.Exists(_previewCacheFolder))
            {
                Directory.CreateDirectory(_previewCacheFolder);
            }
        }

        public async Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model)
        {
            if (model.Url == null)
            {
                throw new ArgumentNullException(nameof(model.Url), "URL cannot be null");
            }

            string? filePath = null;
            var result = new ProcessingResult();

            // Create unique session folder for this request
            var sessionId = Guid.NewGuid().ToString("N");
            var sessionFolder = Path.Combine(_outputFolder, sessionId);
            Directory.CreateDirectory(sessionFolder);
            result.SessionFolder = sessionFolder;

            try
            {
                await SendProgressToHubAsync("Starting processing...");

                var previewCacheKey = GetPreviewCacheKey(model.Url.ToString());
                var previewCachePath = string.Empty;
                var hasClipTimestamps = HasClipTimestamps(model);

                await SendProgressToHubAsync("Checking preview cache...");
                var previewFileExists = TryGetCachedPreviewFilePath(previewCacheKey, out previewCachePath);

                var canReusePreview = previewFileExists
                    && ((model.Format == Format.MP4 && model.Quality == Quality.Minimal) || (model.Format == Format.MP3));

                if (canReusePreview)
                {
                    filePath = previewCachePath;
                }
                else
                {
                    var isTikTokUrl = Regex.IsMatch(model.Url.ToString(), Constants.RegexConstants.TiktokUrlRegex);
                    var maxRetires = isTikTokUrl ? 3 : 1;

                    await SendProgressToHubAsync("Starting download...");
                    filePath = await DownloadMediaFileAsync(
                        model.Url.ToString(),
                        model.Format,
                        model.Quality,
                        async (progress) => await SendProgressToHubAsync(progress),
                        maxRetires,
                        outputFolder: sessionFolder);
                    await SendProgressToHubAsync("Download completed.");
                }

                if (model.Format == Format.MP3 && Path.GetExtension(filePath)?.ToLower() != ".mp3")
                {
                    await SendProgressToHubAsync("Converting to MP3...");
                    filePath = await ConvertToAudioAsync(filePath, async (progress) => await SendProgressToHubAsync(progress));
                    await SendProgressToHubAsync("MP3 conversion completed.");
                }

                if (hasClipTimestamps)
                {
                    await SendProgressToHubAsync("Cutting clip...");
                    filePath = await CutFileAsync(
                        filePath,
                        model.Url.ToString(),
                        model.StartTimestamp!,
                        model.EndTimestamp!,
                        model.Format,
                        async (progress) => await SendProgressToHubAsync(progress));
                    await SendProgressToHubAsync("Clip cutting completed.");
                }

                await SendProgressToHubAsync("Preparing file for download...");

                var fileModel = new FileModel
                {
                    FilePath = filePath,
                    Name = RemoveIdFromFileName(Path.GetFileName(filePath))
                };

                result.IsSuccessful = true;
                result.FileModel = fileModel;

                await SendProgressToHubAsync("Processing completed successfully!");
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                Debug.WriteLine(ex, "An error occurred while processing the file.");

                CleanupSessionFolder(sessionFolder);

                throw;
            }

            return result;
        }

        public async Task<PreviewMediaResult> GetPreviewMediaAsync(Uri url)
        {
            var result = new PreviewMediaResult();

            try
            {
                var isTikTokUrl = Regex.IsMatch(url.ToString(), Constants.RegexConstants.TiktokUrlRegex);
                var previewCacheKey = GetPreviewCacheKey(url.ToString());

                if (!TryGetCachedPreviewFilePath(previewCacheKey, out var cachedFilePath))
                {
                    var previewQuality = Quality.Minimal;
                    cachedFilePath = await DownloadMediaFileAsync(
                        url.ToString(),
                        Format.MP4,
                        previewQuality,
                        async (progress) => await SendProgressToHubAsync(progress),
                        maxRetries: isTikTokUrl ? 3 : 1,
                        outputFolder: _previewCacheFolder,
                        fileNamePrefix: previewCacheKey);
                }

                result.IsSuccessful = !string.IsNullOrWhiteSpace(cachedFilePath);
                result.StreamUrl = cachedFilePath;
                result.ContentType = "video/mp4";
                result.IsLocalFile = true;

                if (!result.IsSuccessful)
                {
                    result.ErrorMessage = "Unable to download preview.";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                Debug.WriteLine(ex, "An error occurred while resolving preview media.");
            }

            return result;
        }

        private async Task SendProgressToHubAsync(string progress)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
        }

        private async Task<string> CutFileAsync(string filePath, string inputUrl, string startTime, string endTime, Format format, Action<string> onProgress)
        {
            var argsList = new List<string>();
            var clipDuration = GetClipDuration(startTime, endTime, inputUrl);
            var clipLength = FormatTimeSpanForFfmpeg(clipDuration);

            var inputArg = $"-i \"{filePath}\"";
            var cutArg = $"-ss {startTime} -t {clipLength}";

            var outputExtension = format == Format.MP3 ? "mp3" : "mp4";
            var baseFileName = RemoveIdFromFileName(Path.GetFileNameWithoutExtension(filePath));
            var outputFileName = $"{baseFileName}.{outputExtension}";

            var sessionFolder = Path.GetDirectoryName(filePath)!;
            var outputFilePath = Path.Combine(sessionFolder, outputFileName);

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            argsList.Add(inputArg);
            argsList.Add(cutArg);

            if (format == Format.MP3)
            {
                argsList.Add("-vn");
                argsList.Add("-c:a libmp3lame");
            }
            else
            {
                argsList.Add("-c:v libx264");
                argsList.Add("-preset veryfast");
                argsList.Add("-crf 18");
                argsList.Add("-c:a aac");
                argsList.Add("-movflags +faststart");
            }

            argsList.Add($"\"{outputFilePath}\"");

            var argsString = string.Join(" ", argsList);

            using (var process = new Process())
            {
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = argsString;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                // FFmpeg reports progress as error output
                process.ErrorDataReceived += (sender, args) =>
                {
                    var output = args.Data;
                    if (!string.IsNullOrEmpty(output))
                    {
                        var timePattern = @"time=(\d{2}:\d{2}:\d{2})"; // Regex to get the current video time
                        var match = Regex.Match(output, timePattern);
                        if (match.Success)
                        {
                            var time = match.Groups[1].Value;
                            onProgress?.Invoke($"Processing your clip: {time} / {clipLength}");
                        }
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"FFmpeg process exited with code {process.ExitCode}");
                }
            }

            return outputFilePath;
        }

        private static TimeSpan GetClipDuration(string startTime, string endTime, string inputUrl)
        {
            TimeSpan startTimeSpan = TimeSpan.Parse(startTime);
            TimeSpan endTimeSpan = TimeSpan.Parse(endTime);

            TimeSpan diff = endTimeSpan - startTimeSpan;
            if (diff <= TimeSpan.Zero)
            {
                throw new ArgumentException("End timestamp must be greater than start timestamp.");
            }


            diff = diff.Add(TimeSpan.FromMilliseconds(200));

            return diff;
        }

        private static string FormatTimeSpanForFfmpeg(TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"hh\:mm\:ss\.fff");
        }


        private static bool HasClipTimestamps(MediaFileModel model)
        {
            return !string.IsNullOrEmpty(model.StartTimestamp)
                && !string.IsNullOrEmpty(model.EndTimestamp);
        }

        private async Task<string> DownloadMediaFileAsync(
            string inputUrl,
            Format outputFormat,
            Quality outputQuality,
            Action<string> onProgress,
            int maxRetries = 1,
            string? outputFolder = null,
            string? fileNamePrefix = null)
        {
            var argsList = new List<string>();

            var urlArg = inputUrl;
            var fileId = fileNamePrefix ?? Guid.NewGuid().ToString();
            var targetOutputFolder = outputFolder ?? _outputFolder;
            var outputTemplate = Path.Combine(targetOutputFolder, $"{fileId}_%(title).90s.%(ext)s");
            var outputArg = $"-o \"{outputTemplate}\"";

            argsList.Add("--no-playlist");
            argsList.Add("--no-warnings");

            argsList.Add(urlArg);
            argsList.Add(outputArg);

            if (outputFormat == Format.MP3)
            {
                var audioArg = "-x --audio-format mp3";
                argsList.Add(audioArg);
            }
            else
            {
                if (outputQuality == Quality.Minimal ||
                    Regex.IsMatch(inputUrl, Constants.RegexConstants.InstagramUrlRegex) ||
                    Regex.IsMatch(inputUrl, Constants.RegexConstants.TiktokUrlRegex)) // For now TikTok and Instagram do not support higher quality
                {
                    var qualityArg = "-S  vcodec:h264,res:360,ext:mp4:m4a"; // 360p
                    argsList.Add(qualityArg);
                }
                else
                {
                    var qualityArg = "-S vcodec:h264,res,ext:mp4:m4a"; // this wont download 4k. 4k is only available in webm format
                    argsList.Add(qualityArg);
                }

                argsList.Add("--recode mp4");
            }

            if (Regex.IsMatch(inputUrl, Constants.RegexConstants.TiktokUrlRegex))
            {
                var tikTokFixArg = "-f \"b[url!^='https://www.tiktok.com/']\""; // this fixes TikTok downloading bug https://github.com/yt-dlp/yt-dlp/issues/11034
                argsList.Add(tikTokFixArg);
            }

            var argsString = string.Join(" ", argsList);
            string? filePath = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _youtubeDlpPath;
                    process.StartInfo.Arguments = argsString;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, args) =>
                    {
                        var output = args.Data;
                        if (!string.IsNullOrEmpty(output))
                        {
                            if (output.StartsWith("[download]"))
                            {
                                var parts = output.Split(' ');
                                var trimmed = string.Join(" ", parts.Skip(1)); // Skip the '[download]' prefix
                                onProgress?.Invoke($"Downloading: {trimmed}");
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        filePath = Directory.GetFiles(targetOutputFolder)
                            .Single(file => Path.GetFileName(file).StartsWith(fileId));
                        return filePath;
                    }

                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException($"Yt-dlp process exited with code {process.ExitCode}");
                    }

                    await Task.Delay(1000); // Sleep for 1 second before retrying
                }
            }

            throw new InvalidOperationException("Unexpected error occurred during download.");
        }

        private async Task<string> ConvertToAudioAsync(string inputFilePath, Action<string> onProgress)
        {
            var outputFilePath = Path.ChangeExtension(inputFilePath, ".mp3");

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            var argsList = new List<string>
            {
                $"-i \"{inputFilePath}\"",
                "-vn",
                "-c:a libmp3lame",
                $"\"{outputFilePath}\""
            };

            var argsString = string.Join(" ", argsList);
            using (var process = new Process())
            {
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = argsString;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;

                // FFmpeg reports progress as error output
                process.ErrorDataReceived += (sender, args) =>
                {
                    var output = args.Data;
                    if (!string.IsNullOrEmpty(output))
                    {
                        onProgress?.Invoke($"Converting to audio: {output}");
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"FFmpeg process exited with code {process.ExitCode}");
                }
            }

            return outputFilePath;
        }

        private bool TryGetCachedPreviewFilePath(string previewCacheKey, out string previewCachePath)
        {
            previewCachePath = string.Empty;

            if (!Directory.Exists(_previewCacheFolder))
            {
                return false;
            }

            previewCachePath = Directory.GetFiles(_previewCacheFolder, $"{previewCacheKey}_*.mp4")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? string.Empty;

            return !string.IsNullOrWhiteSpace(previewCachePath);
        }

        private static string GetPreviewCacheKey(string inputUrl)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(inputUrl));

            return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
        }

        public void CleanupSessionFolder(string sessionFolder)
        {
            if (string.IsNullOrEmpty(sessionFolder))
            {
                return;
            }

            // Don't delete preview cache folder
            if (sessionFolder.StartsWith(_previewCacheFolder, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                if (Directory.Exists(sessionFolder))
                {
                    Directory.Delete(sessionFolder, recursive: true);
                }
            }
            catch (Exception ex)
            {
                // Ignore cleanup errors - the response is already prepared
            }
        }

        private string RemoveIdFromFileName(string input)
        {
            int firstUnderscoreIndex = input.IndexOf('_');

            if (firstUnderscoreIndex == -1)
            {
                return input;
            }

            return input.Substring(firstUnderscoreIndex + 1);
        }
    }
}
