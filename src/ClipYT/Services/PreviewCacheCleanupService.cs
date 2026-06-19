namespace ClipYT.Services
{
    public class PreviewCacheCleanupService : BackgroundService
    {
        private const int CleanupTimeHour = 3;
        private const int CleanupTimeMinute = 0;
        private const int RetentionDays = 1;

        private readonly string _previewCacheFolder;

        public PreviewCacheCleanupService(IConfiguration configuration)
        {
            var outputFolder = configuration["Config:OutputFolder"] ?? throw new ArgumentNullException(nameof(configuration), "Config:OutputFolder is missing");
            _previewCacheFolder = Path.Combine(outputFolder, "preview-cache");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextCleanupTime = GetNextCleanupTime(now);
                var delay = nextCleanupTime - now;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    CleanupOldPreviewFiles();
                }
            }
        }

        private DateTime GetNextCleanupTime(DateTime currentTime)
        {
            var nextCleanup = currentTime.Date.AddHours(CleanupTimeHour).AddMinutes(CleanupTimeMinute);

            if (currentTime >= nextCleanup)
            {
                nextCleanup = nextCleanup.AddDays(1);
            }

            return nextCleanup;
        }

        private void CleanupOldPreviewFiles()
        {
            if (!Directory.Exists(_previewCacheFolder))
            {
                return;
            }

            var retentionThreshold = DateTime.Now.AddDays(-RetentionDays);

            try
            {
                var files = Directory.GetFiles(_previewCacheFolder);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);

                        if (fileInfo.CreationTime < retentionThreshold)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
