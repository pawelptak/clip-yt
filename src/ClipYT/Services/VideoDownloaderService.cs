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
                RecodeVideo = model.Format == Enums.Format.MP4 ? VideoRecodeFormat.Mp4 : VideoRecodeFormat.None,
                AudioFormat = AudioConversionFormat.Mp3,
                Format = "best"
            };

            if (!string.IsNullOrEmpty(model.StartTimestamp) && !string.IsNullOrEmpty(model.EndTimestamp)) {
                options.Downloader = "ffmpeg";
                options.DownloaderArgs = $"ffmpeg_i:-ss {model.StartTimestamp} -to {model.EndTimestamp}";
            }

            var res = model.Format == Enums.Format.MP4 ? 
                await _youtubeDl.RunVideoDownload(model.Url.ToString(), overrideOptions: options) : 
                await _youtubeDl.RunAudioDownload(model.Url.ToString(), overrideOptions: options);

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
