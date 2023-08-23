using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class VideoModel
    {
        [Required]
        [RegularExpression(@"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$", ErrorMessage = "The provided input is not a valid YouTube URL.")]
        public Uri Url { get; set; }

        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Invalid time format.")]
        public string? Start { get; set; }

        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Invalid time format.")]
        public string? End { get; set; }
    }
}
