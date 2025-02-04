
using AspNetCoreWebApp.Models;
using Newtonsoft.Json;
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
        //private readonly string _uploadPath = @"C:\temp_\yeet - Renamed\";       // New folder for renamed files, not needed with blobstorage configured

        int startNumber = 1;   // Start of the range, not sure if needed 
        int curNumber;  // Current book number, not sure if needed 

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

            _localFolder = Path.Combine(Directory.GetCurrentDirectory(), "Temp"); // Finding if there is a folder for downloading temporary files, such as our books
            // Ensure the Temp folder exists, if not, create it
            if (!Directory.Exists(_localFolder))
            {
                Directory.CreateDirectory(_localFolder);
                _logger.LogInformation($"Created Temp folder at: {_localFolder}");
            }
        }
        private async Task<byte[]> DownloadFileFromServerAsync(string fileName)
        {
            string fileUrl = $"http://192.168.1.122:8080/{fileName}"; // Change to your actual host IP
            _logger.LogInformation($"Downloading from {fileUrl}");

            using (var response = await _httpClient.GetAsync(fileUrl))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to download {fileName}: {response.StatusCode}");
                    return null;
                }
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task DownloadBooksAsync(int startBookNumber, int maxBooks, CancellationToken cancellationToken)
        {
            var booksData = new List<DataModel>();

            for (int bookNumber = startBookNumber; bookNumber <= maxBooks; bookNumber++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                string originalFileName = $"{bookNumber:D4}.zip"; // Format the file name as "0001.zip", "0002.zip", etc.
                //string sourceFilePath = Path.Combine(_sourcePath, originalFileName); // Local path to the file
                string destinationPath = Path.Combine(_localFolder, originalFileName);

                try
                {
                    // Check if the file exists in the local folder
                    /*if (!File.Exists(sourceFilePath))
                    {
                        _logger.LogInformation($"File {originalFileName} not found. Stopping download.");
                        _logger.LogWarning($"No more files found. Expected {originalFileName} in {_sourcePath}, but it does not exist.");
                        break;
                    }
                    */

                    _logger.LogInformation($"Downloading {originalFileName}...");

                    // Download the file
                    byte[] fileBytes = await DownloadFileFromServerAsync(originalFileName);
                    if (fileBytes == null) 
                        continue;
                    await File.WriteAllBytesAsync(destinationPath, fileBytes);


                    _logger.LogInformation($"{originalFileName} downloaded successfully!");

                    DataModel bookMetadata = await RenameFileAsync(destinationPath, bookNumber);

                    string newFileName = $"{bookMetadata.Author} - {bookMetadata.Title} ({bookMetadata.Year}).zip";
                    newFileName = newFileName.Replace(" ", "_"); // Replace spaces with underscore to avoid errors with hyperlinks
                    string renamedFilePath = Path.Combine(_localFolder, newFileName);
                    
                    File.Move(destinationPath, renamedFilePath); // Rewrite the file with the new name
                    
                    // Upload to Blob Storage
                    string blobUrl = await _blobStorageService.UploadFileAsync(renamedFilePath);

                    // Add the URL to the metadata
                    bookMetadata.Url = blobUrl;

                    // Collect data to save later
                    booksData.Add(bookMetadata);

                    File.Delete(renamedFilePath);

                    _logger.LogInformation($"Renamed {originalFileName} -> {newFileName} and uploaded to Blob Storage. URL: {blobUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing {originalFileName}: {ex.Message}");
                }
            }

            string jsonOutput = JsonConvert.SerializeObject(booksData, Formatting.Indented);
            File.WriteAllText("books_metadata.json", jsonOutput);
            _logger.LogInformation("Saved metadata to books_metadata.json");

        }

        private async Task<DataModel> RenameFileAsync(string zipFilePath, int bookSequenceNumber)
        {
            _logger.LogInformation($"Extracting text for renaming from ZIP: {zipFilePath}");

            try
            {
                // Extract the first .txt file from the ZIP
                string extractedText = ExtractFirstTxtFile(zipFilePath);
                if (string.IsNullOrEmpty(extractedText))
                {
                    _logger.LogError($"No valid .txt file found in {zipFilePath}. Keeping original name.");
                    //return Path.GetFileName(zipFilePath); // Keep original name on failure
                    return new DataModel
                    {
                        Author = "Unknown",
                        Title = "Unknown",
                        Year = 0,
                        BookSequenceNumber = bookSequenceNumber
                    };
                }

                // Send the extracted text to the Python API for renaming
                var renameInfo = await _pythonApiService.GetRenamingInfoAsync(extractedText);
                if (renameInfo == null)
                {
                    _logger.LogWarning($"Renaming service failed, keeping original filename.");
                    return new DataModel
                    {
                        Author = "Unknown",
                        Title = "Unknown",
                        Year = 0,
                        BookSequenceNumber = bookSequenceNumber
                    };
                }


                // Create a structured model
                return new DataModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Author = renameInfo.Author,
                    Title = renameInfo.Title,
                    Year = renameInfo.Year,
                    BookSequenceNumber = bookSequenceNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting metadata from {zipFilePath}: {ex.Message}");
                return new DataModel
                {
                    Author = "Unknown",
                    Title = "Unknown",
                    Year = 0,
                    BookSequenceNumber = bookSequenceNumber
                };
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

