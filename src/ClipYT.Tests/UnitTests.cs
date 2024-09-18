using ClipYT.Models;
using ClipYT.Services;
using Microsoft.Extensions.Configuration;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ClipYT.Tests
{
    public class UnitTests
    {
        private readonly MediaFileProcessingService _mediaFileProcessingService;
        private readonly TrackSeparationService _trackSeparationService;
        private readonly string _outputFolder;

        public UnitTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var solutionDirectory = Directory.GetParent(currentDirectory).Parent.Parent.Parent.FullName;
            var clipYTProjectDirectory = Path.Combine(solutionDirectory, "ClipYT");

            var ffmpegPath = Path.Combine(clipYTProjectDirectory, "Utilities", "ffmpeg.exe");
            var youtubeDlpPath = Path.Combine(clipYTProjectDirectory, "Utilities", "yt-dlp.exe");
            var pythonPath = @"C:\Users\jpawl\AppData\Local\Programs\Python\Python39\python.exe"; // Replace with a correct Python exe
            var outputFolder = Path.Combine(clipYTProjectDirectory, "Output");

            var inMemorySettings = new Dictionary<string, string>
            {
                { "Config:FFmpegPath", ffmpegPath },
                { "Config:YoutubeDlpPath", youtubeDlpPath },
                { "Config:PythonPath", pythonPath },            
                { "Config:OutputFolder", outputFolder }
            };
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

            var trackSeparationService = new TrackSeparationService(configuration);
            _trackSeparationService = trackSeparationService;
            _mediaFileProcessingService = new MediaFileProcessingService(configuration, trackSeparationService);
            _outputFolder = outputFolder;
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=invalid")]
        [InlineData("https://www.tiktok.com/invalid")]
        public async Task Invalid_Input_Url_Should_Return_Error_Message(string invalidUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(invalidUrl) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                () => _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel)
            );

            Assert.Equal("Yt-dlp process exited with code 1", exception.Message);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Downloaded_File_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl) };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }


        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Downloaded_Clip_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20" };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Invalid_Cut_Times_Should_Throw_Exception(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };


            // Act & Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                () => _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel)
            );
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Output_Folder_Should_Be_Empty_After_Processing_Completes(string inputUrl) // Except the .gitkeep file
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20" };

            // Act
            await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);

            // Assert
            var outputFilesExist = Directory.GetFiles(_outputFolder).Any(file => !file.EndsWith(".gitkeep"));
            Assert.False(outputFilesExist, "Output folder is not empty.");
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Output_Folder_Should_Be_Empty_After_Processing_Fails(string inputUrl) // Except the .gitkeep file
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:20", EndTimestamp = "00:00:10" };

            // Act
            try
            {
                await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            }
            catch (OperationCanceledException)
            {
                // Assert
                var outputFilesExist = Directory.GetFiles(_outputFolder).Any(file => !file.EndsWith(".gitkeep"));
                Assert.False(outputFilesExist, "Output folder is not empty.");
            }
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://www.tiktok.com/@rickastleyofficial/video/7081656622094929158")]
        public async Task Downloaded_Mp3_Clip_Should_Have_Size_Larger_Than_Zero(string inputUrl)
        {
            // Arrange
            var mediaFileModel = new MediaFileModel { Url = new Uri(inputUrl), StartTimestamp = "00:00:10", EndTimestamp = "00:00:20", Format = Enums.Format.MP3 };

            // Act
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(mediaFileModel);
            var fileModel = result.FileModel;

            // Assert
            Assert.True(fileModel.Data.Length > 0);
        }


        [Theory]
        [InlineData(@"TestFiles\test_file.mp3")]
        public void Separating_Tracks_Should_Be_Successful(string inputPath)
        {
            Assert.True(File.Exists(inputPath));

            // Arrange
            var bytes = File.ReadAllBytes(inputPath);

            // Act
            var result = _trackSeparationService.SeparateTracks(bytes, 4, "test");

            // Assert
            Assert.True(result.IsSuccessful);
        }
    }
}