using ClipYT.Interfaces;
using ClipYT.Models;
using YoutubeDLSharp;

namespace ClipYT.Services
{
    public class VideoDownloaderService : IVideoDownloaderService
    {
        private readonly IConfiguration _configuration;
        public VideoDownloaderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        async Task<FileModel> IVideoDownloaderService.DownloadYoutubeVideoFromUrlAsync(string url)
        {
            var ytdl = new YoutubeDL
            {
                YoutubeDLPath = _configuration["Config:YoutubeDLPath"],
                FFmpegPath = _configuration["Config:FFmpegPath"],
                OutputFolder = _configuration["Config:OutputFolder"]
            };
            var res = await ytdl.RunVideoDownload(url);
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
