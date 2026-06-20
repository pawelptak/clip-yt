using System.Diagnostics;
using System.Text;

namespace ClipYT.Helpers
{
    public interface IProcessRunner
    {
        Task<ProcessResult> RunAsync(ProcessRunnerOptions options);
    }

    public class ProcessRunner : IProcessRunner
    {
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResult> RunAsync(ProcessRunnerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.FileName))
            {
                throw new ArgumentException("FileName cannot be null or empty", nameof(options.FileName));
            }

            var result = new ProcessResult();
            var processName = Path.GetFileNameWithoutExtension(options.FileName);
            var processId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation(
                "Starting process {ProcessName} [ID: {ProcessId}]. Arguments: {Arguments}",
                processName,
                processId,
                options.Arguments);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using (var process = new Process())
            {
                process.StartInfo.FileName = options.FileName;
                process.StartInfo.Arguments = options.Arguments ?? string.Empty;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        outputBuilder.AppendLine(args.Data);
                        options.OutputDataHandler?.Invoke(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        errorBuilder.AppendLine(args.Data);
                        options.ErrorDataHandler?.Invoke(args.Data);
                    }
                };

                var stopwatch = Stopwatch.StartNew();
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                stopwatch.Stop();

                result.ExitCode = process.ExitCode;
                result.IsSuccess = process.ExitCode == 0;
                result.StandardOutput = outputBuilder.ToString();
                result.StandardError = errorBuilder.ToString();

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Process {ProcessName} [ID: {ProcessId}] completed successfully in {Duration:F2}s",
                        processName,
                        processId,
                        stopwatch.Elapsed.TotalSeconds);
                }
                else
                {
                    var stdoutSummary = outputBuilder.Length > 500
                        ? outputBuilder.ToString()[..500] + "..."
                        : outputBuilder.ToString();
                    var stderrSummary = errorBuilder.Length > 500
                        ? errorBuilder.ToString()[..500] + "..."
                        : errorBuilder.ToString();

                    _logger.LogError(
                        "Process {ProcessName} [ID: {ProcessId}] failed with exit code {ExitCode} after {Duration:F2}s.\n" +
                        "Arguments: {Arguments}\n" +
                        "STDOUT: {Stdout}\n" +
                        "STDERR: {Stderr}",
                        processName,
                        processId,
                        result.ExitCode,
                        stopwatch.Elapsed.TotalSeconds,
                        options.Arguments,
                        string.IsNullOrWhiteSpace(stdoutSummary) ? "(empty)" : stdoutSummary,
                        string.IsNullOrWhiteSpace(stderrSummary) ? "(empty)" : stderrSummary);
                }
            }

            return result;
        }
    }

    public class ProcessRunnerOptions
    {
        public required string FileName { get; set; }
        public string? Arguments { get; set; }
        public Action<string>? OutputDataHandler { get; set; }
        public Action<string>? ErrorDataHandler { get; set; }
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public bool IsSuccess { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}
