using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AspNetCoreWebApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreWebApp.Services
{
    public class PythonApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonApiService> _logger;
        private readonly string _pythonApiUrl;

        public PythonApiService(HttpClient httpClient, ILogger<PythonApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _pythonApiUrl = configuration["PythonApi:BaseUrl"] + "/analyze"; // Read from appsettings.json
        }

        public async Task<RenameResponse?> GetRenamingInfoAsync(string textContent)
        {
            _logger.LogInformation("Sending request to Python API for renaming...");

            try
            {
                var request = new RenameRequest { text = textContent };
                var response = await _httpClient.PostAsJsonAsync(_pythonApiUrl, request);


                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Python API returned an error: {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<RenameResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error communicating with Python API: {ex.Message}");
                return null;
            }
        }
    }
}
