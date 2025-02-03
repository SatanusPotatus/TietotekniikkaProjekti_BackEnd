using AspNetCoreWebApp.Models;
using AspNetCoreWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly BookDownloadService _bookDownloadService; // Add reference to BookDownloadService
        private readonly ILogger<StorageController> _logger;

        public StorageController(BlobStorageService blobStorageService, BookDownloadService bookDownloadService, ILogger<StorageController> logger)
        {
            _blobStorageService = blobStorageService;
            _bookDownloadService = bookDownloadService; // Inject BookDownloadService
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBook([FromForm] BookUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                // Save the uploaded file to a temporary folder (Temp folder)
                string tempFilePath = Path.Combine("Temp", request.File.FileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // Upload the file to Blob Storage
                string blobUrl = await _blobStorageService.UploadFileAsync(tempFilePath);

                // Return the Blob Storage URL in the response
                return Ok(new { Url = blobUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file upload: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // New endpoint for processing individual books
        [HttpPost("process-book")]
        public async Task<IActionResult> ProcessBookAsync(int bookNumber)
        {
            try
            {
                // Call BookDownloadService to process this single book
                await _bookDownloadService.DownloadBooksAsync(bookNumber, bookNumber, CancellationToken.None);

                return Ok($"Book {bookNumber} processed and uploaded to Blob Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing book {bookNumber}: {ex.Message}");
                return StatusCode(500, $"Internal server error processing book {bookNumber}");
            }
        }
    }
}
