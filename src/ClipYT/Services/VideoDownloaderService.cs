using ClipYT.Interfaces;
using ClipYT.Models;
using FFMpegCore;
using NYoutubeDL;

namespace ClipYT.Services
{
    public class VideoDownloaderService : IVideoDownloaderService
    {
        private readonly IConfiguration _configuration;
        private readonly YoutubeDL _nYoutubeDl;
        public VideoDownloaderService(IConfiguration configuration)
        {
            _configuration = configuration;
            _nYoutubeDl = new YoutubeDL(_configuration["Config:YoutubeDLPath"]);
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = _configuration["Config:UtilitiesDirectory"] });
        }

        public async Task<FileModel> DownloadYoutubeVideoFromUrlAsync(VideoModel model)
        {
            ClearOutputDirectory();

            string? extension = null;

            _nYoutubeDl.Options.VideoFormatOptions.Format = NYoutubeDL.Helpers.Enums.VideoFormat.best;

            switch (model.Format)
            {
                case Enums.Format.MP4:
                    extension = nameof(Enums.Format.MP4).ToLower();
                    //extension = ".webm";
                    _nYoutubeDl.Options.VideoFormatOptions.MergeOutputFormat = NYoutubeDL.Helpers.Enums.VideoFormat.mp4;
                    //_nYoutubeDl.Options.PostProcessingOptions.RecodeFormat = NYoutubeDL.Helpers.Enums.VideoFormat.mp4;
                    //_nYoutubeDl.Options.VideoFormatOptions.Format = NYoutubeDL.Helpers.Enums.VideoFormat.mp4;
                    break;

                case Enums.Format.MP3:
                    extension = nameof(Enums.Format.MP3).ToLower();
                    _nYoutubeDl.Options.PostProcessingOptions.ExtractAudio = true;
                    _nYoutubeDl.Options.PostProcessingOptions.KeepVideo = false;
                    _nYoutubeDl.Options.PostProcessingOptions.AudioFormat = NYoutubeDL.Helpers.Enums.AudioFormat.mp3;
                    break;
            }

            var outputPathNoExtension = Path.Combine(_configuration["Config:OutputFolder"], $"%(title)s");
            _nYoutubeDl.Options.FilesystemOptions.Output = outputPathNoExtension;

            await _nYoutubeDl.DownloadAsync(model.Url.ToString());

            string filePath = Directory.GetFiles(_configuration["Config:OutputFolder"]).First(file => !file.EndsWith(".gitkeep"));

            if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp))
            {
                await SaveMediaChunkAsync(filePath, model.StartTimestamp, model.EndTimestamp);
            }

            var fileData = await File.ReadAllBytesAsync(filePath);
            var file = new FileModel
            {
                Data = fileData,
                Name = Path.GetFileName(filePath)
            };

            // TODO: remove old nuget;
            return file;
        }

        private void ClearOutputDirectory()
        {
            DirectoryInfo di = new(_configuration["Config:OutputFolder"]);

            foreach (var file in di.GetFiles())
            {
                if (!file.Name.EndsWith(".gitkeep"))
                {
                    file.Delete();
                }
            }
        }

        private static async Task SaveMediaChunkAsync(string filePath, string startTime, string endTime)
        {
            var outputPath = Path.Join(Path.GetDirectoryName(filePath),$"{Path.GetFileNameWithoutExtension(filePath)}-clip{Path.GetExtension(filePath)}");
            await FFMpeg.SubVideoAsync(filePath, outputPath, TimeSpan.Parse(startTime), TimeSpan.Parse(endTime));
            File.Delete(filePath);
            File.Move(outputPath, filePath);
        }
    }
}