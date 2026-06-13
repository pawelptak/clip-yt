using ClipYT.Interfaces;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class MetadataService : IMetadataService
    {
        private const string TikTokOEmbedUrl = "https://www.tiktok.com/oembed?url={0}";
        private const string YouTubeThumbnailUrl = "https://i.ytimg.com/vi/{0}/hqdefault.jpg";

        private const string OpenGraphTitlePattern = @"<meta\s+property=[""']og:title[""']\s+content=[""']([^""']+)[""']";
        private const string OpenGraphTitleAltPattern = @"<meta\s+content=[""']([^""']+)[""']\s+property=[""']og:title[""']";
        private const string TwitterTitlePattern = @"<meta\s+name=[""']twitter:title[""']\s+content=[""']([^""']+)[""']";
        private const string TitleTagPattern = @"<title>([^<]+)</title>";
        private const string OpenGraphImagePattern = @"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']+)[""']";
        private const string OpenGraphImageAltPattern = @"<meta\s+content=[""']([^""']+)[""']\s+property=[""']og:image[""']";
        private const string TwitterImagePattern = @"<meta\s+name=[""']twitter:image[""']\s+content=[""']([^""']+)[""']";
        private const string TikTokTitleJsonPattern = @"""title""\s*:\s*""([^""]+)""";
        private const string TikTokThumbnailJsonPattern = @"""thumbnail_url""\s*:\s*""([^""]+)""";
        private const int TimeoutSeconds = 5;

        private readonly IHttpClientFactory _httpClientFactory;

        public MetadataService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string?> GetTitleAsync(Uri inputUri)
        {
            var uriString = inputUri.ToString();
            if (Regex.IsMatch(uriString, Constants.RegexConstants.YoutubeUrlRegex))
            {
                var title = await TryGetYouTubeTitleAsync(uriString);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            if (Regex.IsMatch(uriString, Constants.RegexConstants.TiktokUrlRegex))
            {
                var title = await TryGetTikTokOEmbedTitleAsync(uriString);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            if (Regex.IsMatch(uriString, Constants.RegexConstants.TwitterUrlRegex) ||
                Regex.IsMatch(uriString, Constants.RegexConstants.FacebookUrlRegex) ||
                Regex.IsMatch(uriString, Constants.RegexConstants.InstagramUrlRegex))
            {
                var title = await TryGetMetaTitleAsync(uriString);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            var ogTitle = await TryGetOpenGraphTitleAsync(uriString);
            if (!string.IsNullOrWhiteSpace(ogTitle))
            {
                return ogTitle;
            }

            return null;
        }

        public async Task<string?> GetThumbnailUrlAsync(Uri inputUri)
        {
            var uriString = inputUri.ToString();

            if (Regex.IsMatch(uriString, Constants.RegexConstants.YoutubeUrlRegex))
            {
                var videoId = ExtractYouTubeVideoId(uriString);
                if (!string.IsNullOrWhiteSpace(videoId))
                {
                    return string.Format(YouTubeThumbnailUrl, videoId);
                }
            }

            if (Regex.IsMatch(uriString, Constants.RegexConstants.InstagramUrlRegex) || Regex.IsMatch(uriString, Constants.RegexConstants.FacebookUrlRegex))
            {
                var metaThumbnailUrl = await TryGetMetaThumbnailAsync(uriString);
                if (!string.IsNullOrWhiteSpace(metaThumbnailUrl))
                {
                    return metaThumbnailUrl;
                }
            }

            if (Regex.IsMatch(uriString, Constants.RegexConstants.TiktokUrlRegex))
            {
                var tiktokThumbnail = await TryGetTikTokOEmbedThumbnailAsync(uriString);
                if (!string.IsNullOrWhiteSpace(tiktokThumbnail))
                {
                    return tiktokThumbnail;
                }
            }

            var ogThumbnail = await TryGetOpenGraphThumbnailAsync(uriString);
            if (!string.IsNullOrWhiteSpace(ogThumbnail))
            {
                return ogThumbnail;
            }

            return null;
        }
        private async Task<string?> TryGetYouTubeTitleAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                var title = ExtractOpenGraphTitle(html) ?? ExtractTitleTag(html);
                if (title != null)
                {
                    title = Regex.Replace(title, @"\s*-\s*YouTube\s*$", "", RegexOptions.IgnoreCase);
                }

                return title;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetMetaTitleAsync(string url)
        {
            try
            {
                var client = CreateBrowserLikeHttpClient();
                using var request = CreateBrowserLikeRequest(url);
                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                return ExtractOpenGraphTitle(html) ?? ExtractTwitterTitle(html) ?? ExtractTitleTag(html);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetOpenGraphTitleAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                return ExtractOpenGraphTitle(html) ?? ExtractTwitterTitle(html) ?? ExtractTitleTag(html);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetTikTokOEmbedTitleAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var oembedUrl = string.Format(TikTokOEmbedUrl, Uri.EscapeDataString(url));

                using var request = new HttpRequestMessage(HttpMethod.Get, oembedUrl);

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var titleMatch = Regex.Match(json, TikTokTitleJsonPattern);
                if (titleMatch.Success)
                {
                    var title = titleMatch.Groups[1].Value;
                    title = Regex.Unescape(title);
                    return title;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetMetaThumbnailAsync(string url)
        {
            try
            {
                var client = CreateBrowserLikeHttpClient();
                using var request = CreateBrowserLikeRequest(url);
                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                return ExtractOpenGraphImage(html);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetOpenGraphThumbnailAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                return ExtractOpenGraphImage(html) ?? ExtractTwitterImage(html);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGetTikTokOEmbedThumbnailAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var oembedUrl = string.Format(TikTokOEmbedUrl, Uri.EscapeDataString(url));

                using var request = new HttpRequestMessage(HttpMethod.Get, oembedUrl);

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var thumbnailMatch = Regex.Match(json, TikTokThumbnailJsonPattern);
                if (thumbnailMatch.Success)
                {
                    var thumbnailUrl = thumbnailMatch.Groups[1].Value;
                    thumbnailUrl = Regex.Unescape(thumbnailUrl);
                    return thumbnailUrl;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static HttpClient CreateBrowserLikeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            return client;
        }

        private static HttpRequestMessage CreateBrowserLikeRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");

            return request;
        }

        private static string? ExtractOpenGraphTitle(string html)
        {
            var ogTitleMatch = Regex.Match(html, OpenGraphTitlePattern, RegexOptions.IgnoreCase);
            if (ogTitleMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(ogTitleMatch.Groups[1].Value);
            }

            var ogTitleAltMatch = Regex.Match(html, OpenGraphTitleAltPattern, RegexOptions.IgnoreCase);
            if (ogTitleAltMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(ogTitleAltMatch.Groups[1].Value);
            }

            return null;
        }

        private static string? ExtractTwitterTitle(string html)
        {
            var twitterTitleMatch = Regex.Match(html, TwitterTitlePattern, RegexOptions.IgnoreCase);
            if (twitterTitleMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(twitterTitleMatch.Groups[1].Value);
            }

            return null;
        }

        private static string? ExtractTitleTag(string html)
        {
            var titleMatch = Regex.Match(html, TitleTagPattern, RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value);
            }

            return null;
        }

        private static string? ExtractOpenGraphImage(string html)
        {
            var ogImageMatch = Regex.Match(html, OpenGraphImagePattern, RegexOptions.IgnoreCase);
            if (ogImageMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(ogImageMatch.Groups[1].Value);
            }

            var ogImageAltMatch = Regex.Match(html, OpenGraphImageAltPattern, RegexOptions.IgnoreCase);
            if (ogImageAltMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(ogImageAltMatch.Groups[1].Value);
            }

            return null;
        }

        private static string? ExtractTwitterImage(string html)
        {
            var twitterImageMatch = Regex.Match(html, TwitterImagePattern, RegexOptions.IgnoreCase);
            if (twitterImageMatch.Success)
            {
                return System.Net.WebUtility.HtmlDecode(twitterImageMatch.Groups[1].Value);
            }

            return null;
        }

        private static string? ExtractYouTubeVideoId(string url)
        {
            var match = Regex.Match(url, Constants.RegexConstants.YoutubeUrlRegex);
            if (match.Success && match.Groups.Count >= 6)
            {
                return match.Groups[5].Value;
            }

            return null;
        }
    }
}
