
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetCoreWebApp.Services;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreWebApp.Services
{
    public class BookDownloadBackgroundService : BackgroundService
    {
        private readonly ILogger<BookDownloadBackgroundService> _logger;
        private readonly BookDownloadService _bookDownloadService;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // TimeSpan.FromDays(1);
        private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(10);

        public BookDownloadBackgroundService(ILogger<BookDownloadBackgroundService> logger, BookDownloadService bookDownloadService)
        {
            _logger = logger;
            _bookDownloadService = bookDownloadService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookDownloadBackgroundService is starting.");

            // Initial delay of 1 minute before first check
            await Task.Delay(_initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for new books...");

                    int startBookNumber = await GetLastDownloadedBookNumberAsync() + 1;

                    await _bookDownloadService.DownloadBooksAsync(startBookNumber, int.MaxValue, stoppingToken);

                    _logger.LogInformation("Check completed. Sleeping until next run.");
                }
                catch (Exception ex)
                {
                    // handle Exceptions if needed
                    _logger.LogError($"Error in background service: {ex.Message}");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        /*private async Task<int> GetLastDownloadedBookNumberAsync()
        {
            // 🔥 TODO: Implement actual logic to check Blob Storage
            _logger.LogInformation("Fetching last downloaded book number...");
            // If you have any CPU-bound logic that is blocking, you can do this:
            return await Task.Run(() =>
            {
                // Simulating a CPU-bound operation (e.g., processing file metadata)
                return 3000; // This is where you'd actually check your Blob Storage
            });
        }*/
        private async Task<int> GetLastDownloadedBookNumberAsync()
        {
            _logger.LogInformation("Fetching last downloaded book number...");

            // Define the default book number to be used when no files are found or when there's an error
            int defaultBookNumber = 3333;
            string destinationFolder = @"C:\temp_\yeet - Renamed\";

            int lastBookNumber = defaultBookNumber; // Start with the default number

            try
            {
                // Get the files in the destination folder
                var files = Directory.GetFiles(destinationFolder, "*.zip");

                // If no files are found, log and continue with default book number
                if (files.Length == 0)
                {
                    _logger.LogInformation("No files found in the destination folder.");
                }
                else if(files.Length == 3335 - defaultBookNumber)  /////////// DELETE THIS LATER /////////////
                {
                    lastBookNumber = 3335;
                }
                else
                {
                    // Get the most recent file based on LastWriteTime
                    var lastFile = files
                        .Select(file => new FileInfo(file))
                        .OrderByDescending(file => file.LastWriteTime)
                        .FirstOrDefault();

                    if (lastFile != null)
                    {
                        // Extract the book number from the filename
                        var fileName = lastFile.Name;
                        var bookNumberStr = Path.GetFileNameWithoutExtension(fileName); // Get file name without extension

                        if (int.TryParse(bookNumberStr, out lastBookNumber))
                        {
                            _logger.LogInformation($"Last downloaded book number: {lastBookNumber}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to parse book number from filename: {fileName}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No valid files found or error occurred while fetching the last file.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching the last downloaded book number: {ex.Message}");
            }

            return await Task.FromResult(lastBookNumber); // Return the final result
        }
    }
}
