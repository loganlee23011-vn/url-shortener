using Shorten.Data.Models;

namespace Shorten.Data.Repositories
{
    public interface IUrlRepository
    {
        Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode);
        Task<ShortenedUrl> CreateAsync(string originalUrl, string shortCode);
        Task IncrementClickCountAsync(string shortCode);
    }
}