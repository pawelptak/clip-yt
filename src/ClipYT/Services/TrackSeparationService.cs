using ClipYT.Interfaces;
using ClipYT.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace ClipYT.Services
{
    public class TrackSeparationService(IConfiguration configuration) : ITrackSeparationService
    {
        private readonly string _outputFolder = configuration["Config:OutputFolder"];
        private readonly string _pythonPath = configuration["Config:PythonPath"];

        // To work on Windows Spleeter requires Python 3.9
        // pip install spleeter
        // pip install numpy==1.26.4
        public ProcessingResult SeparateTracks(byte[] audioBytes, int stemCount, string outputFileName)
        {
            var result = new ProcessingResult();

            var tempAudioPath = Path.Combine(_outputFolder, Path.ChangeExtension(Path.GetFileName(outputFileName), ".wav"));
            File.WriteAllBytes(tempAudioPath, audioBytes);

            var tempOutputPath = Path.Combine(_outputFolder, "spleeter_output");
            var zipOutputPath = Path.Combine(_outputFolder, "spleeter_output.zip");

            if (Directory.Exists(tempOutputPath))
                Directory.Delete(tempOutputPath, true);
            if (File.Exists(zipOutputPath))
                File.Delete(zipOutputPath);

            var processInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $" -m spleeter separate -p spleeter:{stemCount}stems -o \"{tempOutputPath}\" \"{tempAudioPath}\" -f {{filename}}_{{instrument}}.{{codec}}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    result.ErrorMessage = error;

                    return result;
                }
            }

            ZipFile.CreateFromDirectory(tempOutputPath, zipOutputPath);
            var resultBytes = File.ReadAllBytes(zipOutputPath);

            File.Delete(tempAudioPath);
            Directory.Delete(tempOutputPath, true);
            File.Delete(zipOutputPath);

            var returnZip = new FileModel { Name = Path.ChangeExtension(outputFileName, ".zip"), Data = resultBytes };
            result.IsSuccessful = true;
            result.FileModel = returnZip;

            return result;
        }
    }
}
