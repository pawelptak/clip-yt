using ClipYT.Models;
using ClipYT.Services;
using Microsoft.Extensions.Configuration;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ClipYT.Tests
{
    public class UnitTests
    {
        private readonly VideoProcessingService _videoProcessingService;
        private readonly string _outputFolder;

        public UnitTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory).Parent.Parent.Parent.FullName;
            var clipYTProjectDirectory = Path.Combine(solutionDirectory, "ClipYT");

            var ffmpegPath = Path.Combine(clipYTProjectDirectory, "Utilities", "ffmpeg.exe");
            var youtubeDlpPath = Path.Combine(clipYTProjectDirectory, "Utilities", "yt-dlp.exe");
            var outputFolder = Path.Combine(clipYTProjectDirectory, "Output");

            var inMemorySettings = new Dictionary<string, string>
            {
                { "Config:FFmpegPath", ffmpegPath },
                { "Config:YoutubeDlpPath", youtubeDlpPath },
                { "Config:OutputFolder", outputFolder }
            };
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

            _videoProcessingService = new VideoProcessingService(configuration);
            _outputFolder = outputFolder;
        }

        [Fact]
        public async Task Invalid_Yt_Url_Should_Return_Error_Message()
        {
            // Arrange
            var invalidUrl = "https://www.youtube.com/watch?v=invalid";
            var videoModel = new VideoModel { Url = new Uri(invalidUrl) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                () => _videoProcessingService.ProcessYoutubeVideoAsync(videoModel)
            );

            Assert.Equal("Yt-dlp process exited with code 1", exception.Message);
        }

        [Fact]
        public async Task Downloaded_Video_Should_Have_Size_Larger_Than_Zero()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl) };

            // Act
            var fileModel = await _videoProcessingService.ProcessYoutubeVideoAsync(videoModel);

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }

        [Fact]
        public async Task Downloaded_Clip_Should_Have_Size_Larger_Than_Zero()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20" };

            // Act
            var fileModel = await _videoProcessingService.ProcessYoutubeVideoAsync(videoModel);

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }

        [Fact]
        public async Task Invalid_Cut_Times_Should_Throw_Exception()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };


            // Act & Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                () => _videoProcessingService.ProcessYoutubeVideoAsync(videoModel)
            );
        }

        [Fact]
        public async Task Output_Folder_Should_Be_Empty_After_Processing_Completes() // Except the .gitkeep file
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20" };

            // Act
            await _videoProcessingService.ProcessYoutubeVideoAsync(videoModel);

            // Assert
            var outputFilesExist = Directory.GetFiles(_outputFolder).Any(file => !file.EndsWith(".gitkeep"));
            Assert.False(outputFilesExist, "Output folder is not empty.");
        }

        [Fact]
        public async Task Output_Folder_Should_Be_Empty_After_Processing_Fails() // Except the .gitkeep file
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };

            // Act
            try
            {
                await _videoProcessingService.ProcessYoutubeVideoAsync(videoModel);
            }
            catch (OperationCanceledException)
            {
                // Assert
                var outputFilesExist = Directory.GetFiles(_outputFolder).Any(file => !file.EndsWith(".gitkeep"));
                Assert.False(outputFilesExist, "Output folder is not empty.");
            }
        }

        [Fact]
        public async Task Downloaded_Mp3_Clip_Should_Have_Size_Larger_Than_Zero()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoModel = new VideoModel { Url = new Uri(videoUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20", Format = Enums.Format.MP3 };

            // Act
            var fileModel = await _videoProcessingService.ProcessYoutubeVideoAsync(videoModel);

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }

    }
}