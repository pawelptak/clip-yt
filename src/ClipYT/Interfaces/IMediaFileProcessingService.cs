using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IMediaFileProcessingService
    {
        Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model, string? connectionId = null);

        Task<PreviewMediaResult> GetPreviewMediaAsync(Uri url);

        void CleanupSessionFolder(string sessionFolder);
    }
}
