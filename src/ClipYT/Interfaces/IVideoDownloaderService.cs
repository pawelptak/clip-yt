namespace ClipYT.Interfaces
{
    public interface IVideoDownloaderService
    {
        Task DownloadYoutubeVideoFromUrlAsync(string url);
    }
}
