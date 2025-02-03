using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCoreWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCoreWebApp.Services
{
    [ApiController]
    public class PythonApiController : ControllerBase
    {
        private readonly PythonApiService _pythonApiService;
        private readonly ILogger<PythonApiController> _logger;

        public PythonApiController(PythonApiService pythonApiService, ILogger<PythonApiController> logger)
        {
            _pythonApiService = pythonApiService;
            _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> RenameBook([FromBody] RenameRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.text))
            {
                return BadRequest("Invalid request: missing text content.");
            }

            _logger.LogInformation("Sending book content to Python API for renaming...");

            var renameInfo = await _pythonApiService.GetRenamingInfoAsync(request.text);
            if (renameInfo == null)
            {
                return StatusCode(500, "Failed to get rename info from Python API.");
            }

            return Ok(renameInfo);
        }
    }
}
