using ClipYT.Interfaces;
using ClipYT.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace ClipYT.Services
{
    public class VideoDownloaderService : IVideoDownloaderService
    {
        private readonly IConfiguration _configuration;
        private readonly YoutubeDL _youtubeDl;
        public VideoDownloaderService(IConfiguration configuration)
        {
            _configuration = configuration;
            _youtubeDl = new YoutubeDL
            {
                YoutubeDLPath = _configuration["Config:YoutubeDLPath"],
                FFmpegPath = _configuration["Config:FFmpegPath"],
                OutputFolder = _configuration["Config:OutputFolder"]
            };
        }

        public async Task<FileModel> DownloadYoutubeVideoFromUrlAsync(VideoModel model)
        {
            var options = new OptionSet()
            {
                RecodeVideo = VideoRecodeFormat.Mp4,
                Format = "best",
                Downloader = "ffmpeg",
                DownloaderArgs = $"ffmpeg_i:-ss {model.Start} -to {model.End}"
            };

            var res = await _youtubeDl.RunVideoDownload(model.Url.ToString(), overrideOptions: options);
            var fileData = File.ReadAllBytes(res.Data);

            var file = new FileModel
            {
                Data = fileData,
                Name = Path.GetFileName(res.Data)
            };

            File.Delete(res.Data);

            return file;
        }
    }
}
