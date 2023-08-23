using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class VideoModel
    {
        [Required]
        [RegularExpression(@"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$")]
        public Uri Url { get; set; }

        [RegularExpression(@"^(?:[01][0-9]|2[0-3]):[0-0][0-0]:[0-0][0-0]$", ErrorMessage = "Invalid time format and hh:mm:ss values.")]
        public TimeSpan? Start { get; set; }

        [RegularExpression(@"^(?:[01][0-9]|2[0-3]):[0-0][0-0]:[0-0][0-0]$", ErrorMessage = "Invalid time format and hh:mm:ss values.")]
        public TimeSpan? End { get; set; }
    }
}
