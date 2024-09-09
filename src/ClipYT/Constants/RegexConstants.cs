namespace ClipYT.Constants
{
    public static class RegexConstants
    {
        public const string YoutubeUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$";
        public const string TiktokUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m|vm)\.)?((?:tiktok\.com))(\/(?:@[\w\-\.]+\/video\/|v\/))([\d]+)(\S+)?$";
        public const string TimeFormatRegex = @"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$";
    }
}
