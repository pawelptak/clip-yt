using ClipYT.Interfaces;
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
        async Task IVideoDownloaderService.DownloadYoutubeVideoFromUrlAsync(string url)
        {
            var ytdl = new YoutubeDL
            {
                YoutubeDLPath = _configuration["Config:YoutubeDLPath"],
                FFmpegPath = _configuration["Config:FFmpegPath"],
                OutputFolder = _configuration["Config:OutputFolder"]
            };
            var res = await ytdl.RunVideoDownload(url);
            string path = res.Data;
        }
    }
}
