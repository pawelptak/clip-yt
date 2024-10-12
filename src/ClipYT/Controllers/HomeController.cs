using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClipYT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMediaFileProcessingService _mediaFileProcessingService;

        public HomeController(ILogger<HomeController> logger, IMediaFileProcessingService mediaFileProcessingService)
        {
            _logger = logger;
            _mediaFileProcessingService = mediaFileProcessingService;
        }

        public IActionResult Index()
        {
            string version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "Development";
            ViewBag.Version = version;

            var model = new MediaFileModel();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult?> DownloadFile(MediaFileModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(model);

            if (!result.IsSuccessful)
            {
                return null;
            }

            var fileModel = result.FileModel;

            return File(fileModel.Data, System.Net.Mime.MediaTypeNames.Application.Octet, fileModel.Name);
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