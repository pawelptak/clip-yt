﻿using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClipYT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IVideoProcessingService _videoProcessingService;
        private readonly string _basePath;

        public HomeController(ILogger<HomeController> logger, IVideoProcessingService videoProcessingService, IConfiguration configuration)
        {
            _logger = logger;
            _videoProcessingService = videoProcessingService;
            _basePath = configuration["Config:BasePath"];
        }

        public IActionResult Index()
        {
            ViewData["BasePath"] = _basePath;
            var model = new VideoModel();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> DownloadVideo(VideoModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var file = await _videoProcessingService.ProcessYoutubeVideoAsync(model);

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