using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Shorten.Data.Models;
using Shorten.Data.Repositories;
using Shorten.Redirect.Services;

namespace Shorten.Tests
{
    public class UrlServiceTests
    {
        private readonly Mock<IUrlRepository> _mockRepo;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly UrlService _service;

        public UrlServiceTests()
        {
            _mockRepo = new Mock<IUrlRepository>();
            _mockCache = new Mock<IDistributedCache>();
            _service = new UrlService(_mockRepo.Object, _mockCache.Object);
        }

        [Fact]
        public async Task ShortenAsync_ShouldReturnSixCharCode()
        {
            var url = "https://www.google.com";

            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(new ShortenedUrl { OriginalUrl = url, ShortCode = "abc123" });

            _mockCache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.ShortenAsync(url);

            Assert.Equal(6, result.Length);
        }

        [Fact]
        public async Task GetOriginalUrlAsync_ShouldReturnOriginalUrl_WhenCodeExists()
        {
            var shortCode = "abc123";
            var originalUrl = "https://www.google.com";

            _mockCache.Setup(c => c.GetAsync(shortCode, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[]?)null);

            _mockRepo.Setup(r => r.GetByShortCodeAsync(shortCode))
                     .ReturnsAsync(new ShortenedUrl
                     {
                         OriginalUrl = originalUrl,
                         ShortCode = shortCode
                     });

            _mockRepo.Setup(r => r.IncrementClickCountAsync(shortCode))
                     .Returns(Task.CompletedTask);

            _mockCache.Setup(c => c.SetAsync(
                    shortCode,
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetOriginalUrlAsync(shortCode);

            Assert.Equal(originalUrl, result);
        }

        [Fact]
        public async Task GetOriginalUrlAsync_ShouldReturnNull_WhenCodeNotFound()
        {
            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[]?)null);

            _mockRepo.Setup(r => r.GetByShortCodeAsync(It.IsAny<string>()))
                     .ReturnsAsync((ShortenedUrl?)null);

            var result = await _service.GetOriginalUrlAsync("notexist");

            Assert.Null(result);
        }

        [Fact]
        public async Task ShortenAsync_ShouldCallCreateAsync_Once()
        {
            var url = "https://www.example.com";

            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(new ShortenedUrl());

            _mockCache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _service.ShortenAsync(url);

            _mockRepo.Verify(r => r.CreateAsync(url, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetOriginalUrlAsync_ShouldIncrementClickCount_WhenCodeExists()
        {
            var shortCode = "abc123";
            var originalUrl = "https://google.com";

            _mockCache.Setup(c => c.GetAsync(shortCode, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[]?)null);

            _mockRepo.Setup(r => r.GetByShortCodeAsync(shortCode))
                     .ReturnsAsync(new ShortenedUrl
                     {
                         OriginalUrl = originalUrl,
                         ShortCode = shortCode
                     });

            _mockRepo.Setup(r => r.IncrementClickCountAsync(shortCode))
                     .Returns(Task.CompletedTask);

            _mockCache.Setup(c => c.SetAsync(
                    shortCode,
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _service.GetOriginalUrlAsync(shortCode);

            _mockRepo.Verify(r => r.IncrementClickCountAsync(shortCode), Times.Once);
        }

        [Fact]
        public async Task GetOriginalUrlAsync_ShouldReturnCachedValue_WhenCacheHit()
        {
            var shortCode = "abc123";
            var originalUrl = "https://www.google.com";
            var cachedBytes = Encoding.UTF8.GetBytes(originalUrl);

            _mockCache.Setup(c => c.GetAsync(shortCode, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(cachedBytes);

            _mockRepo.Setup(r => r.IncrementClickCountAsync(shortCode))
                     .Returns(Task.CompletedTask);

            var result = await _service.GetOriginalUrlAsync(shortCode);

            Assert.Equal(originalUrl, result);
            _mockRepo.Verify(r => r.GetByShortCodeAsync(It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(r => r.IncrementClickCountAsync(shortCode), Times.Once);
        }
    }
}