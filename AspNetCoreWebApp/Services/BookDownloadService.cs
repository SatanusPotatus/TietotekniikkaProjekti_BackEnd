
///////////////////////////////////////////////// ACTUAL LOGIC ///////////////////////////////////////////////////////

//using System;
//using System.IO;
//using System.Net.Http;
//using System.Threading.Tasks;
//
//namespace AspNetCoreWebApp.Services
//{
//    public class BookDownloadService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _destinationFolder = @"C:\temp_\yeet\"; // Folder to save downloaded files
//        private readonly string _baseUrl = "http://www.lonnrot.net/kirjat/"; // Base URL where files are hosted
//
//        public BookDownloadService(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }
//
//        public async Task DownloadBooksAsync(int startNumber, int endNumber)
//        {
//            if (!Directory.Exists(_destinationFolder))
//            {
//                Directory.CreateDirectory(_destinationFolder);
//            }
//
//            long startTime = DateTime.Now.Ticks;
//
//            for (int curNumber = startNumber; curNumber <= endNumber; curNumber++)
//            {
//                string fileName = $"{curNumber:D4}.zip"; // Format file as "0001.zip"
//                string fileUrl = $"{_baseUrl}{fileName}";
//                string destinationPath = Path.Combine(_destinationFolder, fileName);
//
//                try
//                {
//                    Console.WriteLine($"Downloading {fileName}...");
//                    byte[] fileBytes = await _httpClient.GetByteArrayAsync(fileUrl);
//
//                    // Save the file
//                    await File.WriteAllBytesAsync(destinationPath, fileBytes);
//                    Console.WriteLine($"{fileName} downloaded successfully!");
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Failed to download {fileName}: {ex.Message}");
//                    break; // Stop if there's an error
//                }
//            }
//
//            long elapsedTime = (DateTime.Now.Ticks - startTime) / 10000; // Convert to milliseconds
//            Console.WriteLine($"Total time: {elapsedTime} ms");
//        }
//    }
//}
/////////////////////////////////////////////////// ACTUAL LOGIC ///////////////////////////////////////////////////////



////// TESTING LOGIC ////////

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Services
{
    public class BookDownloadService
    {
        private readonly string _localFolder; // Local folder to save downloaded files
        private readonly string _sourcePath = @"C:\temp_\yeet - Copy\"; // Source folder for testing
        private readonly string _uploadPath = @"C:\temp_\yeet - Renamed\";       // New folder for renamed files
        int startNumber = 1;   // Start of the range
        int curNumber;  // Current book number
        private readonly HttpClient _httpClient;
        private readonly BlobStorageService _blobStorageService;
        private readonly ILogger<BookDownloadService> _logger;
        private readonly PythonApiService _pythonApiService;

        public BookDownloadService(BlobStorageService blobStorageService, IHttpClientFactory httpClientFactory, PythonApiService pythonApiService, ILogger<BookDownloadService> logger)
        {
            _httpClient = httpClientFactory.CreateClient(); // Get a new HttpClient instance
            _blobStorageService = blobStorageService;
            _logger = logger;
            _pythonApiService = pythonApiService;

            _localFolder = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
            // Ensure the Temp folder exists, if not, create it
            if (!Directory.Exists(_localFolder))
            {
                Directory.CreateDirectory(_localFolder);
                _logger.LogInformation($"Created Temp folder at: {_localFolder}");
            }
        }

        public async Task DownloadBooksAsync(int startBookNumber, int maxBooks, CancellationToken cancellationToken)
        {
            for (int bookNumber = startBookNumber; bookNumber <= maxBooks; bookNumber++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                string originalFileName = $"{bookNumber:D4}.zip"; // Format the file name as "0001.zip", "0002.zip", etc.
                string sourceFilePath = Path.Combine(_sourcePath, originalFileName); // Local path to the file
                string destinationPath = Path.Combine(_localFolder, originalFileName);

                try
                {
                    // Check if the file exists in the local folder
                    if (!File.Exists(sourceFilePath))
                    {
                        _logger.LogInformation($"File {originalFileName} not found. Stopping download.");
                        _logger.LogWarning($"No more files found. Expected {originalFileName} in {_sourcePath}, but it does not exist.");
                        break;
                    }

                    _logger.LogInformation($"Downloading {originalFileName}...");

                    // Read the file from the local folder (no network request needed)
                    byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(sourceFilePath), cancellationToken);
                    await File.WriteAllBytesAsync(destinationPath, fileBytes);

                    _logger.LogInformation($"{originalFileName} downloaded successfully!");

                    string renamedFileName = await RenameFileAsync(destinationPath);
                    string renamedFilePath = Path.Combine(_localFolder, renamedFileName);
                    
                    File.Move(destinationPath, renamedFilePath);
                    
                    // Actual upload to Blob Storage instead of local renaming folder
                    string blobUrl = await _blobStorageService.UploadFileAsync(renamedFilePath);  // This will upload the file to Blob Storage

                    File.Delete(renamedFilePath);

                    _logger.LogInformation($"Renamed {originalFileName} -> {renamedFileName} and uploaded to Blob Storage. URL: {blobUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing {originalFileName}: {ex.Message}");
                }
            }
        }

        private async Task<string> RenameFileAsync(string zipFilePath)
        {
            _logger.LogInformation($"Extracting text for renaming from ZIP: {zipFilePath}");

            try
            {
                // Extract the first .txt file from the ZIP
                string extractedText = ExtractFirstTxtFile(zipFilePath);
                if (string.IsNullOrEmpty(extractedText))
                {
                    _logger.LogError($"No valid .txt file found in {zipFilePath}. Keeping original name.");
                    return Path.GetFileName(zipFilePath); // Keep original name on failure
                }

                // Send the extracted text to the Python API for renaming
                var renameInfo = await _pythonApiService.GetRenamingInfoAsync(extractedText);
                if (renameInfo == null)
                {
                    _logger.LogWarning($"Renaming service failed, keeping original filename.");
                    return Path.GetFileName(zipFilePath);
                }

                string newFileName = $"{renameInfo.Author} - {renameInfo.Title} ({renameInfo.Year}).zip";
                _logger.LogInformation($"Renamed to: {newFileName}");

                return newFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {zipFilePath}: {ex.Message}");
                return Path.GetFileName(zipFilePath); // Keep original name on error
            }
        }

        private string ExtractFirstTxtFile(string zipFilePath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    var txtEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                    if (txtEntry == null)
                    {
                        _logger.LogWarning($"No .txt file found inside {zipFilePath}");
                        return string.Empty;
                    }

                    using (var reader = new StreamReader(txtEntry.Open()))
                    {
                        var lines = new List<string>();
                        for (int i = 0; i < 50 && !reader.EndOfStream; i++)
                        {
                            lines.Add(reader.ReadLine());
                        }

                        return string.Join(" ", lines);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting .txt from {zipFilePath}: {ex.Message}");
                return string.Empty;
            }
        }

    }
}

