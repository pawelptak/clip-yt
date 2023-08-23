using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class VideoModel
    {
        [Required]
        [RegularExpression(@"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$", ErrorMessage = "The provided input is not a valid YouTube URL.")]
        public Uri Url { get; set; }

        [RegularExpression(@"^(?:[01][0-9]|2[0-3]):[0-0][0-0]:[0-0][0-0]$", ErrorMessage = "Invalid time format and hh:mm:ss values.")]
        public string? Start { get; set; }

        [RegularExpression(@"^(?:[01][0-9]|2[0-3]):[0-0][0-0]:[0-0][0-0]$", ErrorMessage = "Invalid time format and hh:mm:ss values.")]
        public string? End { get; set; }
    }
}
