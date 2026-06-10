namespace ClipYT.Models
{
    public class PreviewMediaResult
    {
        public bool IsSuccessful { get; set; }

        public string? StreamUrl { get; set; }

        public string? ContentType { get; set; }

        public string? ErrorMessage { get; set; }
    }
}