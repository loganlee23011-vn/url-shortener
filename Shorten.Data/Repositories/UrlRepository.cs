using Microsoft.EntityFrameworkCore;
using Shorten.Data.Models;

namespace Shorten.Data.Repositories
{
    public class UrlRepository : IUrlRepository
    {
        private readonly AppDbContext _context;

        public UrlRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode)
        {
            return await _context.ShortenedUrls
                .FirstOrDefaultAsync(x => x.ShortCode == shortCode);
        }

        public async Task<ShortenedUrl> CreateAsync(string originalUrl, string shortCode)
        {
            var entry = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShortenedUrls.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task IncrementClickCountAsync(string shortCode)
        {
            var entry = await _context.ShortenedUrls
                .FirstOrDefaultAsync(x => x.ShortCode == shortCode);

            if (entry != null)
            {
                entry.ClickCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}