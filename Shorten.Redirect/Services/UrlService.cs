using Shorten.Data.Repositories;

namespace Shorten.Redirect.Services
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _repo;

        public UrlService(IUrlRepository repo)
        {
            _repo = repo;
        }

        public async Task<string> ShortenAsync(string originalUrl)
        {
            // Tạo short code ngẫu nhiên 6 ký tự
            var shortCode = GenerateShortCode();
            await _repo.CreateAsync(originalUrl, shortCode);
            return shortCode;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortCode)
        {
            var entry = await _repo.GetByShortCodeAsync(shortCode);
            if (entry == null) return null;

            await _repo.IncrementClickCountAsync(shortCode);
            return entry.OriginalUrl;
        }

        private string GenerateShortCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
    }
}