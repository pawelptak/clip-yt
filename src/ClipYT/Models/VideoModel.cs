using ClipYT.Constants;
using ClipYT.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClipYT.Models
{
    public class MediaFileModel
    {
        [Required]
        [RegularExpression($"{RegexConstants.YoutubeUrlRegex}|{RegexConstants.TiktokUrlRegex}|{RegexConstants.TwitterUrlRegex}|{RegexConstants.InstagramUrlRegex}|{RegexConstants.FacebookUrlRegex}", ErrorMessage = "The provided input is not a valid URL.")]
        public Uri Url { get; set; }

        public string? StartTimestamp { get; set; }

        public string? EndTimestamp { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Incorrect value.")]
        public int? ClipLength { get; set; }

        public Format Format { get; set; }

        public Quality Quality { get; set; }
    }
}
