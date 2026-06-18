using ClipYT.Enums;
using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class MediaFileProcessingService : IMediaFileProcessingService
    {
        private const string PreviewFormatSelector = "b[height<=360][ext=mp4]/b[height<=360]/b[ext=mp4]/b";

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

            // Ensure preview cache folder exists
            if (!Directory.Exists(_previewCacheFolder))
            {
                Directory.CreateDirectory(_previewCacheFolder);
            }
        }

        public async Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model)
        {
            ClearOutputDirectory();

            string? filePath = null;
            var result = new ProcessingResult();

            try
            {
                if (model.Url == null)
                {
                    throw new ArgumentNullException(nameof(model.Url), "URL cannot be null");
                }

                var isTikTokUrl = Regex.IsMatch(model.Url.ToString(), Constants.RegexConstants.TiktokUrlRegex);
                var previewCachePath = Path.Combine(_previewCacheFolder, "tiktok-preview.mp4");

                // Check if we can reuse TikTok preview file
                var canReusePreview = isTikTokUrl
                    && File.Exists(previewCachePath)
                    && string.IsNullOrEmpty(model.StartTimestamp)
                    && string.IsNullOrEmpty(model.EndTimestamp)
                    && model.Format == Enums.Format.MP4;

                if (canReusePreview)
                {
                    // Reuse the preview file - no need to download again!
                    var fileInfo = new FileInfo(previewCachePath);
                    Debug.WriteLine($"[TikTok Reuse] Using cached preview file: {previewCachePath} ({fileInfo.Length / 1024 / 1024:F2} MB)");
                    await SendProgressToHubAsync("Using cached preview file...");
                    filePath = previewCachePath;
                }
                else
                {
                    // Download normally
                    Debug.WriteLine($"[TikTok Download] Cannot reuse preview. IsTikTok: {isTikTokUrl}, FileExists: {File.Exists(previewCachePath)}, NoTimestamps: {string.IsNullOrEmpty(model.StartTimestamp) && string.IsNullOrEmpty(model.EndTimestamp)}, Format: {model.Format}");
                    var maxRetires = isTikTokUrl ? 3 : 1;
                    filePath = await DownloadMediaFileAsync(model.Url.ToString(), model.Format, model.Quality, async (progress) => await SendProgressToHubAsync(progress), maxRetires);

                    if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp))
                    {
                        await CutAndConvertFileAsync(filePath, model.Url.ToString(), model.StartTimestamp, model.EndTimestamp, model.Format, async (progress) => await SendProgressToHubAsync(progress));
                    }
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                Debug.WriteLine($"[Submit] File read completed. Size: {fileBytes.Length / 1024 / 1024:F2} MB");

                var fileModel = new FileModel
                {
                    Data = fileBytes,
                    Name = RemoveIdFromFileName(Path.GetFileName(filePath))
                };

                result.IsSuccessful = true;
                result.FileModel = fileModel;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                Debug.WriteLine(ex, "An error occurred while processing the file.");

                throw;
            }
            finally
            {
                // Always clean up after submit - including TikTok preview cache
                if (filePath != null && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            return result;
        }

        public async Task<PreviewMediaResult> GetPreviewMediaAsync(Uri url)
        {
            var result = new PreviewMediaResult();

            try
            {
                var isTikTokUrl = Regex.IsMatch(url.ToString(), Constants.RegexConstants.TiktokUrlRegex);

                if (isTikTokUrl)
                {
                    // For TikTok, download to cache and serve from there
                    // This avoids 403 errors from expired URLs
                    var cachedFilePath = await DownloadTikTokPreviewToCacheAsync(url.ToString());
                    result.IsSuccessful = !string.IsNullOrWhiteSpace(cachedFilePath);
                    result.StreamUrl = cachedFilePath;
                    result.ContentType = "video/mp4";
                    result.IsLocalFile = true;

                    if (!result.IsSuccessful)
                    {
                        result.ErrorMessage = "Unable to download TikTok preview.";
                    }
                }
                else
                {
                    // For other platforms (YouTube, Twitter, Instagram), use direct streaming
                    var streamUrl = await ExtractPreviewStreamUrlAsync(url.ToString());
                    result.IsSuccessful = !string.IsNullOrWhiteSpace(streamUrl);
                    result.StreamUrl = streamUrl;
                    result.ContentType = GetPreviewContentType(streamUrl);
                    result.IsLocalFile = false;

                    if (!result.IsSuccessful)
                    {
                        result.ErrorMessage = "Unable to resolve preview stream.";
                    }
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

        private async Task CutAndConvertFileAsync(string filePath, string inputUrl, string startTime, string endTime, Format format, Action<string> onProgress)
        {
            var argsList = new List<string>();
            var clipDuration = GetClipDuration(startTime, endTime, inputUrl);
            var clipLength = FormatTimeSpanForFfmpeg(clipDuration);

            var inputArg = $"-i \"{filePath}\"";
            var cutArg = $"-ss {startTime} -t {clipLength}";

            var outputExtension = format == Format.MP3 ? "mp3" : "mp4";
            var outputFileName = $"{Guid.NewGuid()}.{outputExtension}";
            var outputArg = Path.Combine(_outputFolder, outputFileName);


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

            argsList.Add($"\"{outputArg}\"");

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

            File.Replace(outputArg, filePath, null);
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

        private async Task<string> DownloadMediaFileAsync(string inputUrl, Format outputFormat, Quality outputQuality, Action<string> onProgress, int maxRetries = 1)
        {
            var argsList = new List<string>();

            var urlArg = inputUrl;
            var fileId = Guid.NewGuid();
            var outputArg = $"-o {_outputFolder}/{fileId}_%(title).90s.%(ext)s";

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
                        filePath = Directory.GetFiles(_outputFolder)
                            .Single(file => Path.GetFileName(file).StartsWith(fileId.ToString()));
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

        private async Task<string> ExtractPreviewStreamUrlAsync(string inputUrl)
        {
            var isTikTokUrl = Regex.IsMatch(inputUrl, Constants.RegexConstants.TiktokUrlRegex);

            var argsList = new List<string>
            {
                "--no-playlist",
                "--no-warnings"
            };

            if (isTikTokUrl)
            {
                // Use TikTok-specific format selector to avoid download issues
                argsList.Add("-f \"b[url!^='https://www.tiktok.com/']/b\"");
            }
            else
            {
                argsList.Add($"-f \"{PreviewFormatSelector}\"");
            }

            argsList.Add("-g");
            argsList.Add(inputUrl);

            var argsString = string.Join(" ", argsList);
            var outputLines = new List<string>();
            var errorLines = new List<string>();

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
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        outputLines.Add(args.Data.Trim());
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        errorLines.Add(args.Data.Trim());
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Unable to load video preview.");
                }
            }

            var streamUrl = outputLines.LastOrDefault(line => Uri.TryCreate(line, UriKind.Absolute, out _));

            return streamUrl ?? throw new InvalidOperationException("Unable to get stream URL from preview.");
        }

        private async Task<string> DownloadTikTokPreviewToCacheAsync(string inputUrl)
        {
            // Clean up old previews before starting
            CleanupAllPreviewFiles();

            var cachedFilePath = Path.Combine(_previewCacheFolder, "tiktok-preview.mp4");

            var argsList = new List<string>
            {
                "--no-playlist",
                "--no-warnings",
                "-f \"b[url!^='https://www.tiktok.com/']/b[height<=360]/b\"",
                $"-o \"{cachedFilePath}\"",
                inputUrl
            };

            var argsString = string.Join(" ", argsList);

            using (var process = new Process())
            {
                process.StartInfo.FileName = _youtubeDlpPath;
                process.StartInfo.Arguments = argsString;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Unable to download TikTok preview.");
                }
            }

            if (!File.Exists(cachedFilePath))
            {
                throw new InvalidOperationException("TikTok preview file was not created.");
            }

            return cachedFilePath;
        }

        private void CleanupAllPreviewFiles()
        {
            try
            {
                if (!Directory.Exists(_previewCacheFolder))
                {
                    return;
                }

                var files = Directory.GetFiles(_previewCacheFolder, "*.mp4");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private static string GetPreviewContentType(string? streamUrl)
        {
            if (string.IsNullOrWhiteSpace(streamUrl) || !Uri.TryCreate(streamUrl, UriKind.Absolute, out var streamUri))
            {
                return "video/mp4";
            }

            var extension = Path.GetExtension(streamUri.AbsolutePath).ToLowerInvariant();

            return extension switch
            {
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                _ => "video/mp4"
            };
        }

        private void ClearOutputDirectory()
        {
            if (!Directory.Exists(_outputFolder))
            {
                return;
            }

            DirectoryInfo di = new(_outputFolder);

            foreach (var file in di.GetFiles())
            {
                if (!file.Name.EndsWith(".gitkeep"))
                {
                    file.Delete();
                }
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
