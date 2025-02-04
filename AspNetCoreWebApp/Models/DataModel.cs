using Newtonsoft.Json;

namespace AspNetCoreWebApp.Models
{
    public class DataModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Author { get; set; }
        public string? Title { get; set; }
        public int? Year { get; set; }
        public string? Url { get; set; }
        public int BookSequenceNumber { get; set; }
    }
}
