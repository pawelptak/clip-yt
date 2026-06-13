namespace ClipYT.Interfaces
{
    public interface IUrlValidationService
    {
        Task<bool> IsUrlValidAsync(Uri url);
    }
}
