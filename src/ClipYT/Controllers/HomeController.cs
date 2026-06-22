using ClipYT.Constants;
using ClipYT.Interfaces;
using ClipYT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Diagnostics;

namespace ClipYT.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IMediaFileProcessingService _mediaFileProcessingService;
        private readonly IMetadataService _metadataService;
        private readonly IUrlValidationService _urlValidationService;

        public HomeController(
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache,
            IMediaFileProcessingService mediaFileProcessingService,
            IMetadataService metadataService,
            IUrlValidationService urlValidationService)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _mediaFileProcessingService = mediaFileProcessingService;
            _metadataService = metadataService;
            _urlValidationService = urlValidationService;
        }

        public IActionResult Index()
        {
            string version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "Development";
            ViewBag.Version = version;

            var model = new MediaFileModel();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> DownloadFile(MediaFileModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var connectionId = Request.Form["signalRConnectionId"].FirstOrDefault();
            var result = await _mediaFileProcessingService.ProcessMediaFileAsync(model, connectionId);

            if (!result.IsSuccessful)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    isSuccessful = false,
                    errorMessage = result.ErrorMessage ?? "An error occurred while processing the file."
                });
            }

            var fileModel = result.FileModel;

            if (fileModel == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    isSuccessful = false,
                    errorMessage = "The file could not be generated."
                });
            }

            var stream = new FileStream(fileModel.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                bufferSize: FileStreamConstants.StreamBufferSize, useAsync: true);

            Response.RegisterForDispose(new CleanupCallback(_mediaFileProcessingService, result.SessionFolder!));

            return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, fileModel.Name);
        }

        [HttpGet]
        public async Task<IActionResult> ThumbnailUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var mediaUrl))
            {
                return BadRequest(new { isSuccessful = false, errorMessage = "The provided URL is invalid." });
            }

            if (!await _urlValidationService.IsUrlValidAsync(mediaUrl))
            {
                return BadRequest(new { isSuccessful = false, errorMessage = "The provided URL is not allowed." });
            }

            var thumbnailUrl = await _metadataService.GetThumbnailUrlAsync(mediaUrl);

            return Json(new { isSuccessful = true, thumbnailUrl });
        }

        [HttpGet]
        public async Task<IActionResult> VideoTitle(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var mediaUrl))
            {
                return BadRequest(new { isSuccessful = false, errorMessage = "The provided URL is invalid." });
            }

            if (!await _urlValidationService.IsUrlValidAsync(mediaUrl))
            {
                return BadRequest(new { isSuccessful = false, errorMessage = "The provided URL is not allowed." });
            }

            var title = await _metadataService.GetTitleAsync(mediaUrl);

            return Json(new { isSuccessful = true, title });
        }

        [HttpGet]
        public async Task<IActionResult> PreviewInfo(string url, string? connectionId = null)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var mediaUrl))
            {
                return BadRequest(new PreviewMediaResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "The provided URL is invalid."
                });
            }

            if (!await _urlValidationService.IsUrlValidAsync(mediaUrl))
            {
                return BadRequest(new PreviewMediaResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "The provided URL is not allowed."
                });
            }

            var result = await _mediaFileProcessingService.GetPreviewMediaAsync(mediaUrl, connectionId);
            if (!result.IsSuccessful)
            {
                return BadRequest(result);
            }

            CachePreviewMediaResult(mediaUrl, result);

            return Json(new PreviewMediaResult
            {
                IsSuccessful = true,
                ContentType = result.ContentType,
                StreamUrl = Url.Action(nameof(PreviewStream), new { url = mediaUrl.ToString() })
            });
        }

        [HttpGet]
        public async Task<IActionResult> PreviewStream(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var mediaUrl))
            {
                return BadRequest();
            }

            if (!await _urlValidationService.IsUrlValidAsync(mediaUrl))
            {
                return BadRequest();
            }

            var previewResult = GetCachedPreviewMediaResult(mediaUrl);

            if (previewResult == null)
            {
                previewResult = await _mediaFileProcessingService.GetPreviewMediaAsync(mediaUrl);

                if (!previewResult.IsSuccessful || string.IsNullOrWhiteSpace(previewResult.StreamUrl))
                {
                    return NotFound();
                }

                CachePreviewMediaResult(mediaUrl, previewResult);
            }

            if (!previewResult.IsSuccessful || string.IsNullOrWhiteSpace(previewResult.StreamUrl))
            {
                return NotFound();
            }

            if (previewResult.IsLocalFile)
            {
                return ServeLocalFile(previewResult);
            }

            var client = _httpClientFactory.CreateClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, previewResult.StreamUrl);

            if (Request.Headers.TryGetValue("Range", out var rangeHeaderValues))
            {
                requestMessage.Headers.TryAddWithoutValidation("Range", rangeHeaderValues.ToString());
            }

            using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            if (responseMessage.StatusCode != HttpStatusCode.OK && responseMessage.StatusCode != HttpStatusCode.PartialContent)
            {
                return StatusCode((int)responseMessage.StatusCode);
            }

            Response.StatusCode = (int)responseMessage.StatusCode;
            Response.ContentType = responseMessage.Content.Headers.ContentType?.ToString() ?? previewResult.ContentType ?? "video/mp4";

            CopyResponseHeader(responseMessage.Content.Headers.ContentLength, "Content-Length");
            CopyResponseHeader(responseMessage.Content.Headers.ContentRange?.ToString(), "Content-Range");
            CopyResponseHeader(responseMessage.Headers.AcceptRanges.FirstOrDefault(), "Accept-Ranges");
            CopyResponseHeader(responseMessage.Headers.ETag?.ToString(), "ETag");
            CopyResponseHeader(responseMessage.Content.Headers.LastModified?.ToString("R"), "Last-Modified");

            await using var responseStream = await responseMessage.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            await responseStream.CopyToAsync(Response.Body, HttpContext.RequestAborted);

            return new EmptyResult();
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

        private void CopyResponseHeader(object? value, string headerName)
        {
            if (value == null)
            {
                return;
            }

            var headerValue = value.ToString();
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                return;
            }

            Response.Headers[headerName] = headerValue;
        }

        private PreviewMediaResult? GetCachedPreviewMediaResult(Uri mediaUrl)
        {
            var cacheKey = GetPreviewCacheKey(mediaUrl);

            if (_memoryCache.TryGetValue(cacheKey, out PreviewMediaResult? cachedPreviewMediaResult))
            {
                return cachedPreviewMediaResult;
            }

            return null;
        }

        private void CachePreviewMediaResult(Uri mediaUrl, PreviewMediaResult previewMediaResult)
        {
            if (!previewMediaResult.IsSuccessful || string.IsNullOrWhiteSpace(previewMediaResult.StreamUrl))
            {
                return;
            }

            var cacheKey = GetPreviewCacheKey(mediaUrl);
            var cachedPreviewMediaResult = new PreviewMediaResult
            {
                IsSuccessful = true,
                StreamUrl = previewMediaResult.StreamUrl,
                ContentType = previewMediaResult.ContentType,
                IsLocalFile = previewMediaResult.IsLocalFile
            };

            _memoryCache.Set(cacheKey, cachedPreviewMediaResult, TimeSpan.FromMinutes(5));
        }

        private static string GetPreviewCacheKey(Uri mediaUrl)
        {
            return $"preview-media:{mediaUrl}";
        }

        private IActionResult ServeLocalFile(PreviewMediaResult previewResult)
        {
            if (!System.IO.File.Exists(previewResult.StreamUrl))
            {
                return NotFound();
            }

            var fileStream = new FileStream(
                previewResult.StreamUrl, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                FileStreamConstants.StreamBufferSize,
                FileOptions.Asynchronous);

            return new FileStreamResult(fileStream, previewResult.ContentType ?? "video/mp4")
            {
                EnableRangeProcessing = true
            };
        }
    }

    internal class CleanupCallback : IDisposable
    {
        private readonly IMediaFileProcessingService _service;
        private readonly string _sessionFolder;

        public CleanupCallback(IMediaFileProcessingService service, string sessionFolder)
        {
            _service = service;
            _sessionFolder = sessionFolder;
        }

        public void Dispose()
        {
            _service.CleanupSessionFolder(_sessionFolder);
        }
    }
}