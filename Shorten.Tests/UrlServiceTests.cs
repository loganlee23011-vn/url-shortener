using Moq;
using Shorten.Data.Models;
using Shorten.Data.Repositories;
using Shorten.Redirect.Services;

namespace Shorten.Tests
{
    public class UrlServiceTests
    {
        private readonly Mock<IUrlRepository> _mockRepo;
        private readonly UrlService _service;

        public UrlServiceTests()
        {
            _mockRepo = new Mock<IUrlRepository>();
            _service = new UrlService(_mockRepo.Object);
        }

        // Test 1: ShortenAsync phải trả về shortCode 6 ký tự
        [Fact]
        public async Task ShortenAsync_ShouldReturnSixCharCode()
        {
            // Arrange
            var url = "https://www.google.com";
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(new ShortenedUrl { OriginalUrl = url, ShortCode = "abc123" });

            // Act
            var result = await _service.ShortenAsync(url);

            // Assert
            Assert.Equal(6, result.Length);
        }

        // Test 2: GetOriginalUrlAsync trả về URL gốc khi shortCode tồn tại
        [Fact]
        public async Task GetOriginalUrlAsync_ShouldReturnOriginalUrl_WhenCodeExists()
        {
            // Arrange
            var shortCode = "abc123";
            var originalUrl = "https://www.google.com";

            _mockRepo.Setup(r => r.GetByShortCodeAsync(shortCode))
                     .ReturnsAsync(new ShortenedUrl { OriginalUrl = originalUrl, ShortCode = shortCode });

            _mockRepo.Setup(r => r.IncrementClickCountAsync(shortCode))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetOriginalUrlAsync(shortCode);

            // Assert
            Assert.Equal(originalUrl, result);
        }

        // Test 3: GetOriginalUrlAsync trả về null khi shortCode không tồn tại
        [Fact]
        public async Task GetOriginalUrlAsync_ShouldReturnNull_WhenCodeNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByShortCodeAsync(It.IsAny<string>()))
                     .ReturnsAsync((ShortenedUrl?)null);

            // Act
            var result = await _service.GetOriginalUrlAsync("notexist");

            // Assert
            Assert.Null(result);
        }

        // Test 4: ShortenAsync phải gọi CreateAsync đúng 1 lần
        [Fact]
        public async Task ShortenAsync_ShouldCallCreateAsync_Once()
        {
            // Arrange
            var url = "https://www.example.com";
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(new ShortenedUrl());

            // Act
            await _service.ShortenAsync(url);

            // Assert
            _mockRepo.Verify(r => r.CreateAsync(url, It.IsAny<string>()), Times.Once);
        }

        // Test 5: GetOriginalUrlAsync phải tăng click count khi tìm thấy
        [Fact]
        public async Task GetOriginalUrlAsync_ShouldIncrementClickCount_WhenCodeExists()
        {
            // Arrange
            var shortCode = "abc123";
            _mockRepo.Setup(r => r.GetByShortCodeAsync(shortCode))
                     .ReturnsAsync(new ShortenedUrl { OriginalUrl = "https://google.com", ShortCode = shortCode });
            _mockRepo.Setup(r => r.IncrementClickCountAsync(shortCode))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.GetOriginalUrlAsync(shortCode);

            // Assert
            _mockRepo.Verify(r => r.IncrementClickCountAsync(shortCode), Times.Once);
        }
    }
}