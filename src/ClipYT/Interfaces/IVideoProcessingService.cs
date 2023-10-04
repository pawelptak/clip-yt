using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IVideoProcessingService
    {
        Task<FileModel> ProcessYoutubeVideoAsync(VideoModel model);
    }
}
