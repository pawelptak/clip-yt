using ClipYT.Constants;
using ClipYT.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class VideoModel
    {
        public VideoModel()
        {
            ClipLength = 10;
        }

        [Required]
        [RegularExpression(RegexConstants.YoutubeUrlRegex, ErrorMessage = "The provided input is not a valid YouTube URL.")]
        public Uri Url { get; set; }

        [RegularExpression(RegexConstants.TimeFormatRegex, ErrorMessage = "Invalid time format.")]
        public string? StartTimestamp { get; set; }

        [RegularExpression(RegexConstants.TimeFormatRegex, ErrorMessage = "Invalid time format.")]
        public string? EndTimestamp { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Incorrect value.")]
        public int? ClipLength { get; set; }

        public Format Format { get; set; }

        public Quality Quality { get; set; }
    }
}
