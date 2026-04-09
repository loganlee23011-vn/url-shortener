namespace Shorten.Data.Models
{
    public class ShortenedUrl
    {
        public int Id { get; set; }
        public string OriginalUrl { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ClickCount { get; set; } = 0;
    }
}