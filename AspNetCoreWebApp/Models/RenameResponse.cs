namespace AspNetCoreWebApp.Models
{
    public class RenameResponse
    {
        public string Title { get; set; } = "Unknown Title";
        public string Author { get; set; } = "Unknown Author";
        public int Year { get; set; } = 0;
        public string? Url { get; set; } // Todnäk heitetään Db omaan modeliin tai nimetään koko paska uusiks 

        public string GetRenamedFileName()
        {
            // Ensure a valid filename (remove any invalid characters)
            string safeAuthor = RemoveInvalidFileChars(Author);
            string safeTitle = RemoveInvalidFileChars(Title);

            return $"{safeAuthor} - {safeTitle} ({Year}).zip";
        }

        private static string RemoveInvalidFileChars(string input)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c.ToString(), "");
            }
            return input.Trim();
        }
    }
}
