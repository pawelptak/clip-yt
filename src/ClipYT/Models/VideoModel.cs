using ClipYT.Constants;
using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class VideoModel
    {
        [Required]
        [RegularExpression(RegexConstants.YoutubeUrlRegex, ErrorMessage = "The provided input is not a valid YouTube URL.")]
        public Uri Url { get; set; }

        [RegularExpression(RegexConstants.TimeformatRegex, ErrorMessage = "Invalid time format.")]
        public string? Start { get; set; }

        [RegularExpression(RegexConstants.TimeformatRegex, ErrorMessage = "Invalid time format.")]
        public string? End { get; set; }
    }
}
