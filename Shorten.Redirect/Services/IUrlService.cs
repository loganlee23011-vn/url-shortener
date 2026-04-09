namespace Shorten.Redirect.Services
{
    public interface IUrlService
    {
        Task<string> ShortenAsync(string originalUrl);
        Task<string?> GetOriginalUrlAsync(string shortCode);
    }
}