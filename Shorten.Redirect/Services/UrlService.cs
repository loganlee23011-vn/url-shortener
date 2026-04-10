using Microsoft.Extensions.Caching.Distributed;
using Shorten.Data.Repositories;

namespace Shorten.Redirect.Services
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _repo;
        private readonly IDistributedCache _cache;

        public UrlService(IUrlRepository repo, IDistributedCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        public async Task<string> ShortenAsync(string originalUrl)
        {
            var shortCode = GenerateShortCode();
            await _repo.CreateAsync(originalUrl, shortCode);

            // Lưu vào cache luôn sau khi tạo
            await _cache.SetStringAsync(shortCode, originalUrl, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

            return shortCode;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortCode)
        {
            // Kiểm tra cache trước
            var cached = await _cache.GetStringAsync(shortCode);
            if (cached != null)
            {
                // Cache hit — không cần query DB
                await _repo.IncrementClickCountAsync(shortCode);
                return cached;
            }

            // Cache miss — query DB
            var entry = await _repo.GetByShortCodeAsync(shortCode);
            if (entry == null) return null;

            // Lưu vào cache cho lần sau
            await _cache.SetStringAsync(shortCode, entry.OriginalUrl, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

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