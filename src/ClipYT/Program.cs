using ClipYT.Helpers;
using ClipYT.Interfaces;
using ClipYT.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
const string LogPath = "Output/logs/app-.log";
const int MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10 MB
const int MaxRetainedLogFiles = 10;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: LogPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: MaxRetainedLogFiles,
        fileSizeLimitBytes: MaxLogFileSizeBytes,
        rollOnFileSizeLimit: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient(string.Empty)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IUrlValidationService, UrlValidationService>();
builder.Services.AddSingleton<IMetadataService, MetadataService>();
builder.Services.AddSingleton<IMediaFileProcessingService, MediaFileProcessingService>();
builder.Services.AddSingleton<IRandomCaptionService, RandomCaptionService>();
builder.Services.AddSingleton<IHolidayService, HolidayService>();
builder.Services.AddSingleton<ITradingSundayService, TradingSundayService>();
builder.Services.AddHostedService<PreviewCacheCleanupService>();

builder.Services.AddSignalR();
var app = builder.Build();

var basePath = builder.Configuration.GetValue<string>("Config:BasePath");

if (!string.IsNullOrEmpty(basePath))
{
    app.UsePathBase(basePath);
}

app.MapHub<ProgressHub>("/progressHub");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}