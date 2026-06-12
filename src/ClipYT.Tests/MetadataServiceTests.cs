using ClipYT.Interfaces;
using ClipYT.Services;

namespace ClipYT.Tests
{
    public class MetadataServiceTests
    {
        private readonly IMetadataService _metadataService;

        public MetadataServiceTests()
        {
            var httpClientFactory = new HttpClientFactory();
            _metadataService = new MetadataService(httpClientFactory);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task MetadataService_Should_Return_Valid_Thumbnail_Url(string inputUrl)
        {
            // Arrange
            var url = new Uri(inputUrl);

            // Act
            var thumbnailUrl = await _metadataService.GetThumbnailUrlAsync(url);

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(thumbnailUrl);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var imageData = await response.Content.ReadAsByteArrayAsync();

            // Assert
            Assert.NotNull(thumbnailUrl);
            Assert.True(Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var resultUri), $"Thumbnail URL is not a valid URI: {thumbnailUrl}");
            Assert.True(resultUri.Scheme == Uri.UriSchemeHttp || resultUri.Scheme == Uri.UriSchemeHttps, "Thumbnail URL must use HTTP or HTTPS scheme");
            Assert.True(response.IsSuccessStatusCode, $"Failed to download thumbnail from {thumbnailUrl}. Status code: {response.StatusCode}");
            Assert.NotNull(contentType);
            Assert.True(contentType.StartsWith("image/"), $"Content-Type is not an image type: {contentType}");
            Assert.True(imageData.Length > 0, "Downloaded image data is empty");
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task MetadataService_Should_Return_Valid_Title(string inputUrl)
        {
            // Arrange
            var url = new Uri(inputUrl);

            // Act
            var title = await _metadataService.GetTitleAsync(url);

            // Assert
            Assert.NotNull(title);
            Assert.NotEmpty(title);
        }

        private sealed class HttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }
    }
}
