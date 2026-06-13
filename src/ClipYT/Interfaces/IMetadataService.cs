namespace ClipYT.Interfaces
{
    public interface IMetadataService
    {
        Task<string?> GetThumbnailUrlAsync(Uri inputUri);
        Task<string?> GetTitleAsync(Uri inputUri);
    }
}
