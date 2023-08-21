using ClipYT.Interfaces;
using ClipYT.Models;
using YoutubeDLSharp;

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

        public async Task<FileModel> DownloadYoutubeVideoFromUrlAsync(Uri url)
        {
            var res = await _youtubeDl.RunVideoDownload(url.ToString());
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
