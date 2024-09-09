namespace ClipYT.Models
{
    public class ProcessingResult
    {
        public bool IsSuccessful { get; set; }
        public FileModel FileModel { get; set; }
        public string ErrorMessage { get; set; }
    }

}
