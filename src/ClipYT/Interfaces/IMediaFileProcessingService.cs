using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IMediaFileProcessingService
    {
        Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model);
    }
}
