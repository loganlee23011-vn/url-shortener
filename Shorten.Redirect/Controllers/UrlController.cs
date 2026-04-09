using Microsoft.AspNetCore.Mvc;
using Shorten.Redirect.Services;

namespace Shorten.Redirect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly IUrlService _urlService;

        public UrlController(IUrlService urlService)
        {
            _urlService = urlService;
        }

        // POST api/url/shorten
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] string originalUrl)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
                return BadRequest("URL không được để trống.");

            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
                return BadRequest("URL không hợp lệ.");

            var shortCode = await _urlService.ShortenAsync(originalUrl);
            var shortUrl = $"{Request.Scheme}://{Request.Host}/api/url/r/{shortCode}";

            return Ok(new { shortUrl, shortCode });
        }

        // GET api/url/r/{shortCode}
        [HttpGet("r/{shortCode}")]
        public async Task<IActionResult> RedirectToUrl(string shortCode)
        {
            var originalUrl = await _urlService.GetOriginalUrlAsync(shortCode);

            if (originalUrl == null)
                return NotFound("Short URL không tồn tại.");

            return base.Redirect(originalUrl);
        }
    }
}