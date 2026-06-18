using ClipYT.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ClipYT.Tests
{
    public class HybridPreviewTests
    {
        private readonly MediaFileProcessingService _mediaFileProcessingService;

        public HybridPreviewTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.Parent?.Parent?.Parent?.FullName ?? throw new InvalidOperationException("Unable to find solution directory");
            var clipYTProjectDirectory = Path.Combine(solutionDirectory, "ClipYT");

            var ffmpegPath = Path.Combine(clipYTProjectDirectory, "Utilities", "ffmpeg.exe");
            var youtubeDlpPath = Path.Combine(clipYTProjectDirectory, "Utilities", "yt-dlp.exe");
            var outputFolder = Path.Combine(clipYTProjectDirectory, "Output");

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Config:FFmpegPath", ffmpegPath },
                { "Config:YoutubeDlpPath", youtubeDlpPath },
                { "Config:OutputFolder", outputFolder }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var hubContextMock = new Mock<IHubContext<ProgressHub>>();
            var clientsMock = new Mock<IHubClients>();
            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

            _mediaFileProcessingService = new MediaFileProcessingService(configuration, hubContextMock.Object);
        }

        [Fact]
        public async Task TikTok_Uses_Local_Cache()
        {
            // Arrange
            var tiktokUrl = new Uri("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158");

            // Act
            var result = await _mediaFileProcessingService.GetPreviewMediaAsync(tiktokUrl);

            // Assert
            Assert.True(result.IsSuccessful, $"Expected success, got: {result.ErrorMessage}");
            Assert.NotNull(result.StreamUrl);
            Assert.True(result.IsLocalFile, "TikTok should use local cache");
            Assert.True(File.Exists(result.StreamUrl), $"Cache file should exist: {result.StreamUrl}");
            Assert.Equal("video/mp4", result.ContentType);

            var fileInfo = new FileInfo(result.StreamUrl);
            Assert.True(fileInfo.Length > 1000, $"File should be substantial, got {fileInfo.Length} bytes");
        }

        [Fact]
        public async Task YouTube_Uses_Direct_Streaming()
        {
            // Arrange
            var youtubeUrl = new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

            // Act
            var result = await _mediaFileProcessingService.GetPreviewMediaAsync(youtubeUrl);

            // Assert
            Assert.True(result.IsSuccessful, $"Expected success, got: {result.ErrorMessage}");
            Assert.NotNull(result.StreamUrl);
            Assert.False(result.IsLocalFile, "YouTube should NOT use local cache");
            Assert.True(result.StreamUrl.StartsWith("http"), "YouTube should return HTTP URL");
        }

        [Fact]
        public async Task TikTok_Always_Downloads_Fresh()
        {
            // Arrange
            var tiktokUrl = new Uri("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158");

            // Act - First call
            var result1 = await _mediaFileProcessingService.GetPreviewMediaAsync(tiktokUrl);
            Assert.True(result1.IsSuccessful);

            // Verify it's a local file and exists
            Assert.True(result1.IsLocalFile);
            Assert.True(File.Exists(result1.StreamUrl));
            Assert.Equal("tiktok-preview.mp4", Path.GetFileName(result1.StreamUrl));
        }
    }
}
