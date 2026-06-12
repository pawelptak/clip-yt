namespace ClipYT.Interfaces
{
    public interface IThumbnailService
    {
        Task<string?> GetThumbnailUrlAsync(Uri url);
    }
}
