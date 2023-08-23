namespace ClipYT.Constants
{
    public static class RegexConstants
    {
        public const string YoutubeUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$";
        public const string TimeformatRegex = @"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$";
    }
}
