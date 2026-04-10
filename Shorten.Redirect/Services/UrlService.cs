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

            try
            {
                await _cache.SetStringAsync(shortCode, originalUrl, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });

                Console.WriteLine($"Cache set success: {shortCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis set error: {ex.Message}");
            }

            return shortCode;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortCode)
        {
            try
            {
                var cached = await _cache.GetStringAsync(shortCode);
                if (cached != null)
                {
                    Console.WriteLine($"Cache hit: {shortCode}");
                    await _repo.IncrementClickCountAsync(shortCode);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis get error: {ex.Message}");
            }

            Console.WriteLine($"Cache miss: {shortCode}");

            var entry = await _repo.GetByShortCodeAsync(shortCode);
            if (entry == null) return null;

            try
            {
                await _cache.SetStringAsync(shortCode, entry.OriginalUrl, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });

                Console.WriteLine($"Cache set success: {shortCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis set error: {ex.Message}");
            }

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