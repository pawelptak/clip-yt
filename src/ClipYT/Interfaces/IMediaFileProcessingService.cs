using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IMediaFileProcessingService
    {
        Task<ProcessingResult> ProcessMediaFileAsync(MediaFileModel model, string? connectionId = null);

        Task<PreviewMediaResult> GetPreviewMediaAsync(Uri url, string? connectionId = null);

        void CleanupSessionFolder(string sessionFolder);
    }
}
