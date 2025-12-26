using System.Net;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace BeatBind.Tests.Infrastructure.Services
{
    public class GithubReleaseServiceTests : IDisposable
    {
        private readonly Mock<ILogger<GithubReleaseService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly GithubReleaseService _service;

        public GithubReleaseServiceTests()
        {
            _mockLogger = new Mock<ILogger<GithubReleaseService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _service = new GithubReleaseService(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetLatestReleaseAsync_WithValidResponse_ShouldReturnRelease()
        {
            // Arrange
            var releaseJson = """
            {
                "tag_name": "v2.1.0",
                "html_url": "https://github.com/justinknguyen/BeatBind/releases/tag/v2.1.0",
                "name": "Version 2.1.0",
                "published_at": "2025-01-15T10:00:00Z",
                "prerelease": false
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, releaseJson);

            // Act
            var result = await _service.GetLatestReleaseAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Version.Should().Be("2.1.0");
            result.Name.Should().Be("Version 2.1.0");
            result.Url.Should().Contain("github.com");
            result.IsPrerelease.Should().BeFalse();
        }

        [Fact]
        public async Task GetLatestReleaseAsync_WithPrerelease_ShouldReturnPrerelease()
        {
            // Arrange
            var releaseJson = """
            {
                "tag_name": "v2.1.0-beta",
                "html_url": "https://github.com/justinknguyen/BeatBind/releases/tag/v2.1.0-beta",
                "name": "Version 2.1.0 Beta",
                "published_at": "2025-01-15T10:00:00Z",
                "prerelease": true
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, releaseJson);

            // Act
            var result = await _service.GetLatestReleaseAsync();

            // Assert
            result.Should().NotBeNull();
            result!.IsPrerelease.Should().BeTrue();
        }

        [Fact]
        public async Task GetLatestReleaseAsync_WhenHttpRequestFails_ShouldReturnNull()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.GetLatestReleaseAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLatestReleaseAsync_WithInvalidJson_ShouldReturnNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "invalid json");

            // Act
            var result = await _service.GetLatestReleaseAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLatestReleaseAsync_WithEmptyResponse_ShouldReturnNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "null");

            // Act
            var result = await _service.GetLatestReleaseAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void IsNewerVersion_WhenLatestIsNewer_ShouldReturnTrue()
        {
            // Act
            var result = _service.IsNewerVersion("2.0.0", "2.1.0");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNewerVersion_WhenLatestIsSame_ShouldReturnFalse()
        {
            // Act
            var result = _service.IsNewerVersion("2.0.0", "2.0.0");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNewerVersion_WhenLatestIsOlder_ShouldReturnFalse()
        {
            // Act
            var result = _service.IsNewerVersion("2.1.0", "2.0.0");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNewerVersion_WithVPrefix_ShouldStripAndCompare()
        {
            // Act
            var result = _service.IsNewerVersion("v2.0.0", "v2.1.0");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNewerVersion_WithMixedVPrefix_ShouldStripAndCompare()
        {
            // Act
            var result = _service.IsNewerVersion("v2.0.0", "2.1.0");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNewerVersion_WithInvalidCurrentVersion_ShouldReturnFalse()
        {
            // Act
            var result = _service.IsNewerVersion("invalid", "2.1.0");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNewerVersion_WithInvalidLatestVersion_ShouldReturnFalse()
        {
            // Act
            var result = _service.IsNewerVersion("2.0.0", "invalid");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNewerVersion_WithPatchVersionDifference_ShouldReturnTrue()
        {
            // Act
            var result = _service.IsNewerVersion("2.0.0", "2.0.1");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNewerVersion_WithMajorVersionDifference_ShouldReturnTrue()
        {
            // Act
            var result = _service.IsNewerVersion("1.9.9", "2.0.0");

            // Assert
            result.Should().BeTrue();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
