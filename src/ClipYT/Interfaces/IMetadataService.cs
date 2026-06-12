namespace ClipYT.Interfaces
{
    public interface IMetadataService
    {
        Task<string?> GetThumbnailUrlAsync(Uri url);
        Task<string?> GetTitleAsync(Uri url);
    }
}
