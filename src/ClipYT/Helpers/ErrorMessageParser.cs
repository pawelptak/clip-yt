using System.Text.RegularExpressions;

namespace ClipYT.Helpers
{
    public static class ErrorMessageParser
    {
        public static string ParseYtDlpError(string stderr, string stdout)
        {
            if (string.IsNullOrWhiteSpace(stderr) && string.IsNullOrWhiteSpace(stdout))
            {
                return "Unable to download video. Please check the URL and try again.";
            }

            var combinedOutput = $"{stderr}\n{stdout}";

            // Age-restricted or login required
            if (Regex.IsMatch(combinedOutput, @"This post may not be comfortable for some audiences|Log in for access|--cookies-from-browser|authentication", RegexOptions.IgnoreCase))
            {
                return "This content is age-restricted or requires authentication. Unable to download.";
            }

            // Video unavailable
            if (Regex.IsMatch(combinedOutput, @"Video unavailable|This video is unavailable|Private video|This video has been removed", RegexOptions.IgnoreCase))
            {
                return "This video is unavailable or has been removed.";
            }

            // Geo-blocked
            if (Regex.IsMatch(combinedOutput, @"not available in your country|geo-blocked|geographic restriction", RegexOptions.IgnoreCase))
            {
                return "This video is not available in your region.";
            }

            // HTTP errors
            if (Regex.IsMatch(combinedOutput, @"HTTP Error 403"))
            {
                return "Access denied. The video may be private or restricted.";
            }

            if (Regex.IsMatch(combinedOutput, @"HTTP Error 404"))
            {
                return "Video not found. The link may be invalid or expired.";
            }

            if (Regex.IsMatch(combinedOutput, @"HTTP Error 429"))
            {
                return "Too many requests. Please wait a moment and try again.";
            }

            // Network errors
            if (Regex.IsMatch(combinedOutput, @"Connection refused|Connection timed out|Network is unreachable", RegexOptions.IgnoreCase))
            {
                return "Network connection failed. Please check your internet connection.";
            }

            // Invalid URL
            if (Regex.IsMatch(combinedOutput, @"Unsupported URL|Invalid URL|Unable to extract", RegexOptions.IgnoreCase))
            {
                return "Invalid or unsupported video URL.";
            }

            // Try to extract ERROR: message
            var errorMatch = Regex.Match(combinedOutput, @"ERROR:\s*(?:\[[\w\s]+\]\s*)?(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
            if (errorMatch.Success)
            {
                var errorMsg = errorMatch.Groups[1].Value.Trim();

                // Clean up technical details
                errorMsg = Regex.Replace(errorMsg, @"Use --[\w-]+", "", RegexOptions.IgnoreCase);
                errorMsg = Regex.Replace(errorMsg, @"See\s+https?://\S+", "", RegexOptions.IgnoreCase);
                errorMsg = errorMsg.Trim().TrimEnd('.');

                if (!string.IsNullOrWhiteSpace(errorMsg) && errorMsg.Length < 150)
                {
                    return $"Unable to download: {errorMsg}";
                }
            }

            // Generic fallback
            return "Unable to download video. Please try a different video or check the URL.";
        }

        public static string ParseFfmpegError(string stderr)
        {
            if (string.IsNullOrWhiteSpace(stderr))
            {
                return "Video processing failed. Please try again.";
            }

            // File errors
            if (Regex.IsMatch(stderr, @"No such file or directory", RegexOptions.IgnoreCase))
            {
                return "Video file not found. Please try downloading again.";
            }

            // Invalid data
            if (Regex.IsMatch(stderr, @"Invalid data found|moov atom not found", RegexOptions.IgnoreCase))
            {
                return "Video file is corrupted or incomplete. Please try again.";
            }

            // Codec errors
            if (Regex.IsMatch(stderr, @"Unknown encoder|Encoder.*not found", RegexOptions.IgnoreCase))
            {
                return "Video encoding failed. The video format may not be supported.";
            }

            // Out of memory
            if (Regex.IsMatch(stderr, @"Out of memory|Cannot allocate memory", RegexOptions.IgnoreCase))
            {
                return "Insufficient memory to process the video.";
            }

            // Generic fallback
            return "Video processing failed. Please try again.";
        }
    }
}
