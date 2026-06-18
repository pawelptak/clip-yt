using ClipYT.Enums;
using ClipYT.Models;
using ClipYT.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ClipYT.Tests
{
    public class TikTokPreviewReuseTests
    {
        private readonly MediaFileProcessingService _mediaFileProcessingService;
        private readonly string _previewCacheFolder;

        public TikTokPreviewReuseTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory)?.Parent?.Parent?.Parent?.FullName 
                ?? throw new InvalidOperationException("Unable to find solution directory");
            var clipYTProjectDirectory = Path.Combine(solutionDirectory, "ClipYT");

            var ffmpegPath = Path.Combine(clipYTProjectDirectory, "Utilities", "ffmpeg.exe");
            var youtubeDlpPath = Path.Combine(clipYTProjectDirectory, "Utilities", "yt-dlp.exe");
            var outputFolder = Path.Combine(clipYTProjectDirectory, "Output");
            _previewCacheFolder = Path.Combine(outputFolder, "preview-cache");

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
            var clientProxyMock = new Mock<IClientProxy>();
            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

            _mediaFileProcessingService = new MediaFileProcessingService(configuration, hubContextMock.Object);
        }

        [Fact]
        public async Task TikTok_Reuses_Preview_When_No_Cutting_Required()
        {
            // Arrange - Create preview first
            var tiktokUrl = new Uri("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158");
            var previewResult = await _mediaFileProcessingService.GetPreviewMediaAsync(tiktokUrl);

            Assert.True(previewResult.IsSuccessful);
            Assert.True(File.Exists(previewResult.StreamUrl));

            var previewFilePath = previewResult.StreamUrl;
            var previewFileSize = new FileInfo(previewFilePath).Length;
            var previewModifiedTime = File.GetLastWriteTimeUtc(previewFilePath);

            // Wait a bit to ensure timestamp would change if file was re-downloaded
            await Task.Delay(100);

            // Act - Download full file WITHOUT cutting
            var model = new MediaFileModel
            {
                Url = tiktokUrl,
                Format = Format.MP4,
                Quality = Quality.Minimal,
                StartTimestamp = null, // No cutting
                EndTimestamp = null,
                ClipLength = null
            };

            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(model);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.FileModel);

            // File size should match (it was reused, not re-downloaded)
            Assert.Equal(previewFileSize, result.FileModel.Data.Length);

            // Preview file should be deleted after submit (cleanup)
            Assert.False(File.Exists(previewFilePath));
        }

        [Fact]
        public async Task TikTok_Downloads_Fresh_When_Cutting_Required()
        {
            // Arrange - Create preview first
            var tiktokUrl = new Uri("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158");
            var previewResult = await _mediaFileProcessingService.GetPreviewMediaAsync(tiktokUrl);

            Assert.True(previewResult.IsSuccessful);
            var previewFilePath = previewResult.StreamUrl;
            var previewFileSize = new FileInfo(previewFilePath).Length;

            // Act - Download WITH cutting (should NOT reuse preview)
            var model = new MediaFileModel
            {
                Url = tiktokUrl,
                Format = Format.MP4,
                Quality = Quality.Minimal,
                StartTimestamp = "00:00:01", // WITH cutting
                EndTimestamp = "00:00:05",
                ClipLength = 4
            };

            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(model);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.FileModel);

            // Result should be smaller (cut from 5 seconds instead of full video)
            Assert.True(result.FileModel.Data.Length < previewFileSize);

            // Preview file should remain (cutting downloads to Output/, not preview-cache)
            Assert.True(File.Exists(previewFilePath));
        }

        [Fact]
        public async Task TikTok_Downloads_Fresh_When_MP3_Format()
        {
            // Arrange - Create preview first
            var tiktokUrl = new Uri("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158");
            var previewResult = await _mediaFileProcessingService.GetPreviewMediaAsync(tiktokUrl);

            Assert.True(previewResult.IsSuccessful);

            // Act - Download as MP3 (should NOT reuse preview which is MP4)
            var model = new MediaFileModel
            {
                Url = tiktokUrl,
                Format = Format.MP3, // MP3 format
                Quality = Quality.Minimal,
                StartTimestamp = null,
                EndTimestamp = null,
                ClipLength = null
            };

            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(model);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.FileModel);

            // Should be MP3, not MP4
            Assert.True(result.FileModel.Name.EndsWith(".mp3"));
        }
    }
}
