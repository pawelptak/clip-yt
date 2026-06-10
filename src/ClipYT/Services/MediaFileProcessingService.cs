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
        private readonly IHubContext<ProgressHub> _hubContext;

        public MediaFileProcessingService(IConfiguration configuration, IHubContext<ProgressHub> hubContext)
        {
            _ffmpegPath = configuration["Config:FFmpegPath"];
            _youtubeDlpPath = configuration["Config:YoutubeDlpPath"];
            _outputFolder = configuration["Config:OutputFolder"];
            _hubContext = hubContext;
        }

        public async Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model)
        {
            ClearOutputDirectory();

            string filePath = null;
            var result = new ProcessingResult();

            try
            {
                var isTikTokUrl = Regex.IsMatch(model.Url.ToString(), Constants.RegexConstants.TiktokUrlRegex);
                var maxRetires = isTikTokUrl ? 3 : 1; // Downloading TikTok video using yt-dlp sometimes fails, so it is retried
                filePath = await DownloadMediaFileAsync(model.Url.ToString(), model.Format, model.Quality, async (progress) => await SendProgressToHubAsync(progress), maxRetires);

                if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp))
                {
                    await CutAndConvertFileAsync(filePath, model.Url.ToString(), model.StartTimestamp, model.EndTimestamp, model.Format, async (progress) => await SendProgressToHubAsync(progress));
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);

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
                var streamUrl = await ExtractPreviewStreamUrlAsync(url.ToString());
                result.IsSuccessful = !string.IsNullOrWhiteSpace(streamUrl);
                result.StreamUrl = streamUrl;
                result.ContentType = GetPreviewContentType(streamUrl);

                if (!result.IsSuccessful)
                {
                    result.ErrorMessage = "Unable to resolve preview stream.";
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

            argsList.Add(outputArg);

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
            string filePath = null;

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
            var argsList = new List<string>
            {
                "--no-playlist",
                "--no-warnings",
                $"-f \"{PreviewFormatSelector}\"",
                "-g",
                inputUrl
            };

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
                    var errorMessage = errorLines.LastOrDefault() ?? $"Yt-dlp preview resolution exited with code {process.ExitCode}";
                    throw new InvalidOperationException(errorMessage);
                }
            }

            var streamUrl = outputLines.LastOrDefault(line => Uri.TryCreate(line, UriKind.Absolute, out _));

            return streamUrl;
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
