using ClipYT.Enums;
using ClipYT.Interfaces;
using ClipYT.Models;
using System.Diagnostics;

namespace ClipYT.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly string _youtubeDlpPath;
        private readonly string _ffmpegPath;
        private readonly string _outputFolder;

        public VideoProcessingService(IConfiguration configuration)
        {
            _ffmpegPath = configuration["Config:FFmpegPath"];
            _youtubeDlpPath = configuration["Config:YoutubeDlpPath"];
            _outputFolder = configuration["Config:OutputFolder"];
        }

        public async Task<FileModel> ProcessYoutubeVideoAsync(VideoModel model)
        {
            ClearOutputDirectory();

            var filePath = DownloadYoutubeVideo(model.Url.ToString(), model.Format);

            if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp))
            {
                CutAndConvertFile(filePath, model.StartTimestamp, model.EndTimestamp);
            }

            if (string.Equals(Path.GetExtension(filePath), ".webm"))
            {
                filePath = ChangeExtensionToMp4(filePath);
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);

            var fileModel = new FileModel
            {
                Data = fileBytes,
                Name = Path.GetFileName(filePath)
            };

            File.Delete(filePath);

            return fileModel;
        }

        private void CutAndConvertFile(string filePath, string startTime, string endTime)
        {
            var argsList = new List<string>();

            var inputArg = $"-i \"{filePath}\"";
            var cutArg = $"-ss {startTime} -to {AddOneSecond(endTime)}"; // Adding one second is experimental


            var audioConversionArg = $"-c:a copy";

            var outputFileName = "temp.mp4";
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

        private string DownloadYoutubeVideo(string videoUrl, Format outputFormat)
        {
            var argsList = new List<string>();

            var urlArg = videoUrl;
            var outputArg = $"-o {_outputFolder}/%(title)s.%(ext)s";

            argsList.Add(urlArg);
            argsList.Add(outputArg);

            if (outputFormat == Format.MP3)
            {
                var audioArg = "-x --audio-format mp3";
                argsList.Add(audioArg);
            }

            var argsString = string.Join(" ", argsList);

            using (var process = new Process())
            {
                process.StartInfo.FileName = _youtubeDlpPath;
                process.StartInfo.Arguments = argsString;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new OperationCanceledException($"Yt-dlp process exited with code {process.ExitCode}");
                }
            }

            var filePath = Directory.GetFiles(_outputFolder).First(file => !file.EndsWith(".gitkeep"));

            return filePath;
        }

        private void ClearOutputDirectory()
        {
            DirectoryInfo di = new(_outputFolder);

            foreach (var file in di.GetFiles())
            {
                if (!file.Name.EndsWith(".gitkeep"))
                {
                    file.Delete();
                }
            }
        }

        private static string ChangeExtensionToMp4(string filePath)
        {
            var targetExtension = nameof(Format.MP4).ToLower();
            var targetPath = Path.ChangeExtension(filePath, targetExtension);
            File.Move(filePath, targetPath);

            return targetPath;
        }

        private static string AddOneSecond(string timeStamp)
        {
            var seconds = TimeSpan.Parse(timeStamp).TotalSeconds;
            var secondsPlusOne = seconds + 1;
            var timeStampPlusOne = TimeSpan.FromSeconds(secondsPlusOne).ToString();

            return timeStampPlusOne;
        }
    }
}
