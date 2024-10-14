namespace ClipYT.Constants
{
    public static class RegexConstants
    {
        public const string YoutubeUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?$";
        public const string TiktokUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m|vm)\.)?((?:tiktok\.com))(\/(?:@[\w\-\.]+\/video\/|v\/|))([a-zA-Z0-9]+)(\S+)?$";
        public const string TwitterUrlRegex = @"^((?:https?:)?\/\/)?((?:www|mobile\.)?(?:twitter\.com|x\.com))\/([a-zA-Z0-9_]+)\/status\/(\d+)(\/video\/\d+)?(\S+)?$";
        public const string InstagramUrlRegex = @"^((?:https?:)?\/\/)?((?:www|m)\.)?((?:instagram\.com))\/(p|reel|tv)\/([\w\-]+)(\/\S*)?$";
        public const string TimeFormatRegex = @"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$";
    }
}
