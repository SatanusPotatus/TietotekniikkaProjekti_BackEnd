using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = new BlobServiceClient(configuration["AzureBlobStorage:ConnectionString"]);
        _containerName = configuration["AzureBlobStorage:ContainerName"];
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(string filePath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(Path.GetFileName(filePath));

            // Upload the file to BlobStorage
            await blobClient.UploadAsync(filePath, overwrite: true);
            _logger.LogInformation($"File uploaded to BlobStorage: {filePath}");

            // Return the URL of the uploaded file
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading file to BlobStorage: {ex.Message}");
            return string.Empty;
        }
    }
}
