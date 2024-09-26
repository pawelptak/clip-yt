using ClipYT.Enums;
using ClipYT.Interfaces;
using ClipYT.Models;
using System.Diagnostics;
using System.IO.Compression;

namespace ClipYT.Services
{
    public class StemExtractionService(IConfiguration configuration) : IStemExtractionService
    {
        private readonly string _outputFolder = configuration["Config:OutputFolder"];
        private readonly string _pythonPath = configuration["Config:PythonPath"];

        // To work on Windows Spleeter requires Python 3.9, FFmpeg and FFprobe added to Path
        // pip install spleeter
        // pip install numpy==1.26.4
        // TODO: update readme with this info
        public ProcessingResult ExtractStems(byte[] audioBytes, int stemCount, string outputFileName, List<StemType> selectedStems)
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

            // TODO: Fix spleeter error for https://www.youtube.com/watch?v=YBaRFsubJNo&pp=ygUSc3psdWdpIGkga2FsYWZpb3J5 
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

            if (selectedStems.Count < stemCount)
            {
                DeleteUnselectedStems(tempOutputPath, selectedStems);
            }

            byte[] resultBytes;
            string resultFileName;
            var spleeterOutputFiles = Directory.GetFiles(tempOutputPath);
            if (spleeterOutputFiles.Length > 1)
            {
                ZipFile.CreateFromDirectory(tempOutputPath, zipOutputPath);
                resultBytes = File.ReadAllBytes(zipOutputPath);
                resultFileName = Path.ChangeExtension(outputFileName, ".zip");
            }
            else
            {
                var spleeterOutputFile = spleeterOutputFiles.Single();
                resultBytes = File.ReadAllBytes(spleeterOutputFile);
                resultFileName = Path.GetFileName(spleeterOutputFile);
            }

            File.Delete(tempAudioPath);
            Directory.Delete(tempOutputPath, true);
            File.Delete(zipOutputPath);

            var resultFile = new FileModel { Name = resultFileName, Data = resultBytes };
            result.IsSuccessful = true;
            result.FileModel = resultFile;

            return result;
        }

        private void DeleteUnselectedStems(string folderPath, List<StemType> selectedStems)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath) || selectedStems.Count == 0)
            {
                return;
            }

            try
            {
                var files = Directory.GetFiles(folderPath);
                var filesToDelete = files.Where(path =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);

                    return !selectedStems.Any(stemEnum => fileName.EndsWith(Enum.GetName(stemEnum), StringComparison.OrdinalIgnoreCase));
                });

                foreach (var filePath in filesToDelete)
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
