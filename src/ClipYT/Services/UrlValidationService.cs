using ClipYT.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ClipYT.Services
{
    public class UrlValidationService : IUrlValidationService
    {
        private readonly ILogger<UrlValidationService> _logger;

        private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "localhost",
            "127.0.0.1",
            "::1",
            "0.0.0.0",
            "::",
            "*.local",
            "*.localhost"
        };

        public UrlValidationService(ILogger<UrlValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsUrlValidAsync(Uri url)
        {
            try
            {
                // Only allow HTTP and HTTPS schemes
                if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                {
                    return false;
                }

                // Check if URL is from a supported platform
                if (!IsUrlFromSupportedPlatform(url))
                {
                    return false;
                }

                // Check if host is in blocked list
                if (IsHostBlocked(url.Host))
                {
                    return false;
                }

                // Resolve hostname to IP addresses
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(url.Host);

                    // Check each resolved IP address
                    foreach (var ipAddress in hostEntry.AddressList)
                    {
                        if (IsPrivateOrReservedIp(ipAddress))
                        {
                            return false;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    _logger.LogWarning("DNS resolution failed for {Host} with SocketException: {Message}. Allowing URL to proceed as yt-dlp may still handle it", url.Host, ex.Message);
                }

                _logger.LogInformation("URL validation successful for {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during URL validation for {Url}: {Message}", url, ex.Message);

            }

            return true;
        }

        private static bool IsUrlFromSupportedPlatform(Uri url)
        {
            var urlString = url.ToString();

            return Regex.IsMatch(urlString, Constants.RegexConstants.YoutubeUrlRegex) ||
                   Regex.IsMatch(urlString, Constants.RegexConstants.TiktokUrlRegex) ||
                   Regex.IsMatch(urlString, Constants.RegexConstants.TwitterUrlRegex) ||
                   Regex.IsMatch(urlString, Constants.RegexConstants.InstagramUrlRegex) ||
                   Regex.IsMatch(urlString, Constants.RegexConstants.FacebookUrlRegex);
        }

        private static bool IsHostBlocked(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return true;
            }

            // Direct match
            if (BlockedHosts.Contains(host))
            {
                return true;
            }

            // Check for localhost variations
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsPrivateOrReservedIp(IPAddress ipAddress)
        {
            // Convert to bytes for easier checking
            byte[] bytes = ipAddress.GetAddressBytes();

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // IPv4 checks

                // 127.0.0.0/8 - Loopback
                if (bytes[0] == 127)
                {
                    return true;
                }

                // 10.0.0.0/8 - Private
                if (bytes[0] == 10)
                {
                    return true;
                }

                // 172.16.0.0/12 - Private
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                {
                    return true;
                }

                // 192.168.0.0/16 - Private
                if (bytes[0] == 192 && bytes[1] == 168)
                {
                    return true;
                }

                // 169.254.0.0/16 - Link-local
                if (bytes[0] == 169 && bytes[1] == 254)
                {
                    return true;
                }

                // 0.0.0.0/8 - Current network
                if (bytes[0] == 0)
                {
                    return true;
                }

                // 224.0.0.0/4 - Multicast
                if (bytes[0] >= 224 && bytes[0] <= 239)
                {
                    return true;
                }

                // 240.0.0.0/4 - Reserved
                if (bytes[0] >= 240)
                {
                    return true;
                }
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // IPv6 checks

                // ::1 - Loopback
                if (IPAddress.IsLoopback(ipAddress))
                {
                    return true;
                }

                // fe80::/10 - Link-local
                if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                {
                    return true;
                }

                // fc00::/7 - Unique local address
                if ((bytes[0] & 0xfe) == 0xfc)
                {
                    return true;
                }

                // ff00::/8 - Multicast
                if (bytes[0] == 0xff)
                {
                    return true;
                }

                // ::ffff:0:0/96 - IPv4-mapped IPv6 addresses
                if (ipAddress.IsIPv4MappedToIPv6)
                {
                    var mappedIPv4 = ipAddress.MapToIPv4();
                    return IsPrivateOrReservedIp(mappedIPv4);
                }
            }

            return false;
        }
    }
}
