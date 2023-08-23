namespace ClipYT.Models
{
    public class VideoModel
    {
        public Uri Url { get; set; }
        public TimeSpan? Start { get; set; }
        public TimeSpan? End { get; set; }
    }
}
