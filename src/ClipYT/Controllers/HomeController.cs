using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClipYT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IVideoDownloaderService _videoDownloaderService;

        public HomeController(ILogger<HomeController> logger, IVideoDownloaderService videoDownloaderService)
        {
            _logger = logger;
            _videoDownloaderService = videoDownloaderService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DownloadVideo(VideoModel model)
        {
            var file = await _videoDownloaderService.DownloadYoutubeVideoFromUrlAsync(model.Url.ToString());
            return File(file.Data, System.Net.Mime.MediaTypeNames.Application.Octet, file.Name);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}