using ClipYT.Models;
using ClipYT.Controllers;
using ClipYT.Interfaces;
using ClipYT.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ClipYT.Tests
{
    public class UnitTests
    {
        private readonly MediaFileProcessingService _mediaFileProcessingService;
        private readonly string _outputFolder;

        public UnitTests()
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

            var clientProxyMock = new Mock<IClientProxy>();
            clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
            clientProxyMock.Setup(cp => cp.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask); // Thx https://stackoverflow.com/a/56269592

            _mediaFileProcessingService = new MediaFileProcessingService(configuration, hubContextMock.Object);
            _outputFolder = outputFolder;
        }

        [Fact]
        public async Task Preview_Stream_Should_Reuse_Cached_Stream_Url()
        {
            // Arrange
            var mediaUrl = new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
            var previewMediaResult = new PreviewMediaResult
            {
                IsSuccessful = true,
                StreamUrl = "https://cdn.example.com/stream.mp4",
                ContentType = "video/mp4"
            };

            var mediaFileProcessingServiceMock = new Mock<IMediaFileProcessingService>();
            mediaFileProcessingServiceMock
                .Setup(service => service.GetPreviewMediaAsync(mediaUrl))
                .ReturnsAsync(previewMediaResult);

            var responseHandler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("video content"))
            });
            responseHandler.ResponseMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("video/mp4");

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(responseHandler));

            var controller = CreateHomeController(mediaFileProcessingServiceMock.Object, httpClientFactoryMock.Object);
            controller.Url = CreateUrlHelper();

            // Act
            var previewInfoResult = await controller.PreviewInfo(mediaUrl.ToString());
            var previewStreamResult = await controller.PreviewStream(mediaUrl.ToString());

            // Assert
            Assert.IsType<JsonResult>(previewInfoResult);
            Assert.IsType<EmptyResult>(previewStreamResult);
            mediaFileProcessingServiceMock.Verify(service => service.GetPreviewMediaAsync(mediaUrl), Times.Once);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=invalid")]
        [InlineData("https://www.tiktok.com/invalid")]
        [InlineData("https://x.com/i/status/invalid")]
        [InlineData("https://www.instagram.com/invalid")]
        [InlineData("https://www.facebook.com/reel/invalid")]
        public async Task Invalid_Input_Url_Should_Return_Error_Message(string invalidUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(invalidUrl) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel)
            );

            Assert.Equal("Yt-dlp process exited with code 1", exception.Message);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task Downloaded_File_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl) };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.NotNull(fileModel);
            Assert.True(File.Exists(fileModel.FilePath));
            Assert.True(new FileInfo(fileModel.FilePath).Length > 0);
        }


        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task Downloaded_Clip_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20" };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.NotNull(fileModel);
            Assert.True(File.Exists(fileModel.FilePath));
            Assert.True(new FileInfo(fileModel.FilePath).Length > 0);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task Invalid_Cut_Times_Should_Throw_Exception(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };


            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel)
            );
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task Output_Folder_Should_Be_Empty_After_Processing_Fails(string inputUrl) // Except the .gitkeep file
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };

            // Act
            try
            {
                await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            }
            catch (ArgumentException)
            {
                // Assert
                var outputFilesExist = Directory.GetFiles(_outputFolder).Any(file => !file.EndsWith(".gitkeep"));
                Assert.False(outputFilesExist, "Output folder is not empty.");
            }
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task Downloaded_Mp3_Clip_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20", Format = Enums.Format.MP3 };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.NotNull(fileModel);
            Assert.True(File.Exists(fileModel.FilePath));
            Assert.True(new FileInfo(fileModel.FilePath).Length > 0);
        }

        private static HomeController CreateHomeController(IMediaFileProcessingService mediaFileProcessingService, IHttpClientFactory httpClientFactory)
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var thumbnailServiceMock = new Mock<IMetadataService>();
            var urlValidationServiceMock = new Mock<IUrlValidationService>();
            urlValidationServiceMock.Setup(x => x.IsUrlValidAsync(It.IsAny<Uri>())).ReturnsAsync(true);
            var controller = new HomeController(httpClientFactory, memoryCache, mediaFileProcessingService, thumbnailServiceMock.Object, urlValidationServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        private static IUrlHelper CreateUrlHelper()
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(helper => helper.Action(It.IsAny<UrlActionContext>()))
                .Returns("https://localhost/Home/PreviewStream");
            return urlHelperMock.Object;
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            public TestHttpMessageHandler(HttpResponseMessage responseMessage)
            {
                ResponseMessage = responseMessage;
            }

            public HttpResponseMessage ResponseMessage { get; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
            {
                return Task.FromResult(ResponseMessage);
            }
        }

        private sealed class HttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }

        [Fact]
        public async Task UrlValidationService_BlocksUnsupportedPlatforms()
        {
            // Arrange
            var urlValidationService = new UrlValidationService();

            // Act & Assert - Should block arbitrary URLs
            var maliciousUrl = new Uri("https://evil.com/malicious");
            var result1 = await urlValidationService.IsUrlValidAsync(maliciousUrl);
            Assert.False(result1, "Should block non-platform URLs");

            // Should block internal IPs even if scheme is valid
            var internalUrl = new Uri("http://192.168.1.1/admin");
            var result2 = await urlValidationService.IsUrlValidAsync(internalUrl);
            Assert.False(result2, "Should block private IP addresses");

            var localhostUrl = new Uri("http://localhost:8080/api");
            var result3 = await urlValidationService.IsUrlValidAsync(localhostUrl);
            Assert.False(result3, "Should block localhost");
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        [InlineData("https://x.com/i/status/1842206140693664182")]
        [InlineData("https://www.instagram.com/p/DAEQq8lvpvD/")]
        [InlineData("https://www.facebook.com/reel/713709415093896")]
        public async Task UrlValidationService_AllowsSupportedPlatforms(string inputUrl)
        {
            // Arrange
            var urlValidationService = new UrlValidationService();

            // Act & Assert - Should allow supported platforms
            var url = new Uri(inputUrl);
            var result = await urlValidationService.IsUrlValidAsync(url);
            Assert.True(result, $"Should allow URLs like: {inputUrl}");
        }

    }
}