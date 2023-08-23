using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IVideoDownloaderService
    {
        Task<FileModel> DownloadYoutubeVideoFromUrlAsync(VideoModel model);
    }
}
