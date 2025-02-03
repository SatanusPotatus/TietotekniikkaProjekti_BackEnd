using Microsoft.AspNetCore.Http;

namespace AspNetCoreWebApp.Models
{
    public class BookUploadRequest
    {
        public IFormFile File { get; set; }
    }
}
