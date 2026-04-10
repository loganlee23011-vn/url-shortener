using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shorten.Data;
using Shorten.Data.Models;
using Shorten.Redirect.Security;

namespace Shorten.Redirect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters.");
            }

            var normalizedUsername = request.Username.Trim();
            var exists = await _context.UserAccounts.AnyAsync(x => x.Username.ToLower() == normalizedUsername.ToLower());
            if (exists)
            {
                return BadRequest("Username already exists.");
            }

            var user = new UserAccount
            {
                Username = normalizedUsername,
                PasswordHash = PasswordUtility.Hash(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            var token = await CreateSessionAsync(user.Id);
            return Ok(new { token, username = user.Username, role = user.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var normalizedUsername = request.Username.Trim();
            var passwordHash = PasswordUtility.Hash(request.Password);

            var user = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Username.ToLower() == normalizedUsername.ToLower());
            if (user == null || user.PasswordHash != passwordHash)
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = await CreateSessionAsync(user.Id);
            return Ok(new { token, username = user.Username, role = user.Role });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = GetBearerToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var session = await _context.AuthSessions.FirstOrDefaultAsync(x => x.Token == token);
                if (session != null)
                {
                    _context.AuthSessions.Remove(session);
                    await _context.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("Authentication is required.");
            }

            return Ok(new { username = user.Username, role = user.Role });
        }

        [HttpPost("links")]
        public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("Authentication is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OriginalUrl))
            {
                return BadRequest("Original URL is required.");
            }

            if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
            {
                return BadRequest("Original URL is invalid.");
            }

            var shortCode = await GenerateShortCodeAsync();
            var link = new ShortenedUrl
            {
                OriginalUrl = request.OriginalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0,
                UserAccountId = user.Id
            };

            _context.ShortenedUrls.Add(link);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = link.Id,
                originalUrl = link.OriginalUrl,
                shortCode = link.ShortCode,
                shortUrl = $"{Request.Scheme}://{Request.Host}/api/url/r/{link.ShortCode}",
                clickCount = link.ClickCount,
                createdAt = link.CreatedAt
            });
        }

        [HttpGet("links")]
        public async Task<IActionResult> GetLinks()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("Authentication is required.");
            }

            var links = await _context.ShortenedUrls
                .Where(x => x.UserAccountId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    originalUrl = x.OriginalUrl,
                    shortCode = x.ShortCode,
                    shortUrl = $"{Request.Scheme}://{Request.Host}/api/url/r/{x.ShortCode}",
                    clickCount = x.ClickCount,
                    createdAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(links);
        }

        [HttpDelete("links/{id:int}")]
        public async Task<IActionResult> DeleteLink(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("Authentication is required.");
            }

            var link = await _context.ShortenedUrls.FirstOrDefaultAsync(x => x.Id == id && x.UserAccountId == user.Id);
            if (link == null)
            {
                return NotFound("Link was not found.");
            }

            _context.ShortenedUrls.Remove(link);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("admin/overview")]
        public async Task<IActionResult> AdminOverview()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized("Authentication is required.");
            }

            if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var links = await _context.ShortenedUrls
                .Include(x => x.UserAccount)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var overview = links
                .GroupBy(x => x.UserAccount?.Username ?? "Unknown")
                .Select(group => new
                {
                    username = group.Key,
                    totalLinks = group.Count(),
                    totalClicks = group.Sum(x => x.ClickCount),
                    links = group.Select(x => new
                    {
                        id = x.Id,
                        originalUrl = x.OriginalUrl,
                        shortCode = x.ShortCode,
                        shortUrl = $"{Request.Scheme}://{Request.Host}/api/url/r/{x.ShortCode}",
                        clickCount = x.ClickCount,
                        createdAt = x.CreatedAt
                    })
                })
                .OrderByDescending(x => x.totalClicks)
                .ThenByDescending(x => x.totalLinks);

            return Ok(overview);
        }

        private async Task<UserAccount?> GetCurrentUserAsync()
        {
            var token = GetBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var session = await _context.AuthSessions
                .Include(x => x.UserAccount)
                .FirstOrDefaultAsync(x => x.Token == token);

            return session?.UserAccount;
        }

        private string? GetBearerToken()
        {
            var header = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return header["Bearer ".Length..].Trim();
        }

        private async Task<string> CreateSessionAsync(int userId)
        {
            var token = Guid.NewGuid().ToString("N");

            _context.AuthSessions.Add(new AuthSession
            {
                Token = token,
                UserAccountId = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return token;
        }

        private async Task<string> GenerateShortCodeAsync()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            while (true)
            {
                var code = new string(Enumerable.Repeat(chars, 6)
                    .Select(set => set[random.Next(set.Length)])
                    .ToArray());

                var exists = await _context.ShortenedUrls.AnyAsync(x => x.ShortCode == code);
                if (!exists)
                {
                    return code;
                }
            }
        }

        public class AuthRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class CreateLinkRequest
        {
            public string OriginalUrl { get; set; } = string.Empty;
        }
    }
}
