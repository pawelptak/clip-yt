using ClipYT.Interfaces;
using ClipYT.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IMediaFileProcessingService, MediaFileProcessingService>();
builder.Services.AddSingleton<IRandomCaptionService, RandomCaptionService>();
builder.Services.AddSignalR();
var app = builder.Build();

var basePath = builder.Configuration.GetValue<string>("Config:BasePath");

if (!string.IsNullOrEmpty(basePath))
{
    app.UsePathBase(basePath);
}

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

app.MapHub<ProgressHub>("/progressHub");

app.Run();