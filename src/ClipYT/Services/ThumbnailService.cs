using ClipYT.Interfaces;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ThumbnailService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string?> GetThumbnailUrlAsync(Uri url)
        {
            try
            {
                return await ExtractThumbnailUrlAsync(url.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<string?> ExtractThumbnailUrlAsync(string inputUrl)
        {
            // For YouTube, construct thumbnail URL directly (instant)
            if (Regex.IsMatch(inputUrl, Constants.RegexConstants.YoutubeUrlRegex))
            {
                var videoId = ExtractYouTubeVideoId(inputUrl);
                if (!string.IsNullOrWhiteSpace(videoId))
                {
                    return $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";
                }
            }

            if (Regex.IsMatch(inputUrl, Constants.RegexConstants.InstagramUrlRegex) || Regex.IsMatch(inputUrl, Constants.RegexConstants.FacebookUrlRegex))
            {
                var metaThumbnailUrl = await TryGetMetaThumbnailAsync(inputUrl);
                if (!string.IsNullOrWhiteSpace(metaThumbnailUrl))
                {
                    return metaThumbnailUrl;
                }
            }

            // For TikTok, try official oEmbed API
            if (Regex.IsMatch(inputUrl, Constants.RegexConstants.TiktokUrlRegex))
            {
                var tiktokThumbnail = await TryGetTikTokOEmbedThumbnailAsync(inputUrl);
                if (!string.IsNullOrWhiteSpace(tiktokThumbnail))
                {
                    return tiktokThumbnail;
                }
            }

            // For other platforms, try Open Graph tags
            var ogThumbnail = await TryGetOpenGraphThumbnailAsync(inputUrl);
            if (!string.IsNullOrWhiteSpace(ogThumbnail))
            {
                return ogThumbnail;
            }

            return null;
        }

        private async Task<string?> TryGetMetaThumbnailAsync(string url)
        {
            try
            {
                using var handler = new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.All
                };

                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(5);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add headers to mimic a real browser
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                request.Headers.Add("Sec-Fetch-Dest", "document");
                request.Headers.Add("Sec-Fetch-Mode", "navigate");
                request.Headers.Add("Sec-Fetch-Site", "none");

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                var ogImageMatch = Regex.Match(html, @"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                if (ogImageMatch.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(ogImageMatch.Groups[1].Value);
                }

                return null;
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
                client.Timeout = TimeSpan.FromSeconds(10);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();

                // Try og:image first (Open Graph)
                var ogImageMatch = Regex.Match(html, @"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                if (ogImageMatch.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(ogImageMatch.Groups[1].Value);
                }

                // Try twitter:image (Twitter Cards)
                var twitterImageMatch = Regex.Match(html, @"<meta\s+name=[""']twitter:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                if (twitterImageMatch.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(twitterImageMatch.Groups[1].Value);
                }

                // Try alternative og:image format
                var ogImageAltMatch = Regex.Match(html, @"<meta\s+content=[""']([^""']+)[""']\s+property=[""']og:image[""']", RegexOptions.IgnoreCase);
                if (ogImageAltMatch.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(ogImageAltMatch.Groups[1].Value);
                }

                return null;
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
                client.Timeout = TimeSpan.FromSeconds(5);

                // TikTok official oEmbed API
                var oembedUrl = $"https://www.tiktok.com/oembed?url={Uri.EscapeDataString(url)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, oembedUrl);

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var thumbnailMatch = Regex.Match(json, @"""thumbnail_url""\s*:\s*""([^""]+)""");
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
