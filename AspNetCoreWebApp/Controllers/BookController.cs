using AspNetCoreWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreWebApp.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BookController : ControllerBase
    {
        private readonly BookDownloadService _bookDownloadService;

        public BookController(BookDownloadService bookDownloadService)
        {
            _bookDownloadService = bookDownloadService;
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadBooks([FromQuery] int start = 0, [FromQuery] int end = 3335)
        {
            Console.WriteLine($"BookDownloadAPI Triggered with range: {start} to {end}");
            // Get the cancellation token from the request
            var cancellationToken = HttpContext.RequestAborted;

            await _bookDownloadService.DownloadBooksAsync(start, end, cancellationToken);
            return Ok("Download process started!");
        }
    }
}
