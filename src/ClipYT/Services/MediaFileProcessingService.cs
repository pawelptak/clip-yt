﻿using ClipYT.Enums;
using ClipYT.Interfaces;
using ClipYT.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class MediaFileProcessingService : IMediaFileProcessingService
    {
        private readonly string _youtubeDlpPath;
        private readonly string _ffmpegPath;
        private readonly string _outputFolder;

        public MediaFileProcessingService(IConfiguration configuration)
        {
            _ffmpegPath = configuration["Config:FFmpegPath"];
            _youtubeDlpPath = configuration["Config:YoutubeDlpPath"];
            _outputFolder = configuration["Config:OutputFolder"];
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
                filePath = DownloadMediaFile(model.Url.ToString(), model.Format, model.Quality, maxRetires);

                if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp))
                {
                    CutAndConvertFile(filePath, model.StartTimestamp, model.EndTimestamp);
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

        private void CutAndConvertFile(string filePath, string startTime, string endTime)
        {
            var argsList = new List<string>();

            var inputArg = $"-i \"{filePath}\"";
            var cutArg = $"-ss {startTime} -to {endTime}";


            var audioConversionArg = $"-c:a copy";

            var outputFileName = $"{Guid.NewGuid()}.mp4";
            var outputArg = Path.Combine(_outputFolder, outputFileName);


            argsList.Add(inputArg);
            argsList.Add(cutArg);

            // Experimental conversion. Do not delete for now
            //var extensionNoDot = Path.GetExtension(filePath).Replace(".", string.Empty);
            //if (extensionNoDot != nameof(Format.MP3).ToLower())
            //{
            //    var videoConversionArg = $"-c:v libx265 -crf 0 -preset ultrafast";
            //    argsList.Add(videoConversionArg);
            //}

            argsList.Add(audioConversionArg);
            argsList.Add(outputArg);

            var argsString = string.Join(" ", argsList);

            using (var process = new Process())
            {
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = argsString;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new OperationCanceledException($"FFmpeg process exited with code {process.ExitCode}");
                }
            }

            File.Replace(outputArg, filePath, null);
        }

        private string DownloadMediaFile(string inputUrl, Format outputFormat, Quality outputQuality, int maxRetries = 1)
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
                if (outputQuality == Quality.Minimal)
                {
                    var qualityArg = "-S  vcodec:h264,res:360,ext:mp4:m4a --recode mp4"; // 360p
                    argsList.Add(qualityArg);
                }
                else
                {
                    var qualityArg = "-S vcodec:h264,res,ext:mp4:m4a --recode mp4"; // this wont download 4k. 4k is only available in webm format
                    argsList.Add(qualityArg);
                }
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
                    process.Start();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        filePath = Directory.GetFiles(_outputFolder).Single(file => Path.GetFileName(file).StartsWith(fileId.ToString()));

                        return filePath;
                    }

                    if (attempt == maxRetries)
                    {
                        throw new OperationCanceledException($"Yt-dlp process exited with code {process.ExitCode}");
                    }

                    Thread.Sleep(1000); // Sleep for 1 second before retrying
                }
            }

            throw new InvalidOperationException("Unexpected error occurred during download.");
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
