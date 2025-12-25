using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace BeatBind.Tests.Infrastructure.Services
{
    public class SpotifyAuthenticationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<SpotifyAuthenticationService>> _mockLogger;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly SpotifyAuthenticationService _service;

        public SpotifyAuthenticationServiceTests()
        {
            _mockLogger = new Mock<ILogger<SpotifyAuthenticationService>>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _service = new SpotifyAuthenticationService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _httpClient);
        }

        [Fact]
        public async Task AuthenticateAsync_WithMissingClientId_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "",
                ClientSecret = "secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            // Act
            var result = await _service.AuthenticateAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Client ID");
        }

        [Fact]
        public async Task AuthenticateAsync_WithMissingClientSecret_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = ""
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            // Act
            var result = await _service.AuthenticateAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Client Secret");
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            var tokenResponse = """
            {
                "access_token": "new-access-token",
                "token_type": "Bearer",
                "expires_in": 3600,
                "scope": "user-read-playback-state"
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, tokenResponse);

            // Act
            var result = await _service.RefreshTokenAsync("refresh-token");

            // Assert
            result.Success.Should().BeTrue();
            result.AccessToken.Should().Be("new-access-token");
            result.ExpiresIn.Should().Be(3600);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            var errorResponse = """
            {
                "error": "invalid_grant",
                "error_description": "Invalid refresh token"
            }
            """;

            SetupHttpResponse(HttpStatusCode.BadRequest, errorResponse);

            // Act
            var result = await _service.RefreshTokenAsync("invalid-token");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeEmpty();
        }

        [Fact]
        public void IsTokenValid_WithValidToken_ShouldReturnTrue()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // Act
            var result = _service.IsTokenValid(authResult);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsTokenValid_WithExpiredToken_ShouldReturnFalse()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "token",
                ExpiresAt = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var result = _service.IsTokenValid(authResult);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsTokenValid_WithNoAccessToken_ShouldReturnFalse()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // Act
            var result = _service.IsTokenValid(authResult);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void SaveAuthentication_ShouldNotThrow()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 3600,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // Act & Assert - Just verify it doesn't throw
            var act = () => _service.SaveAuthentication(authResult);
            act.Should().NotThrow();
        }

        [Fact]
        public void GetStoredAuthentication_ShouldNotThrow()
        {
            // Act & Assert - Just verify it doesn't throw
            var act = () => _service.GetStoredAuthentication();
            act.Should().NotThrow();
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenHttpClientThrows_ShouldReturnFailure()
        {
            // Arrange
            var config = new ApplicationConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.RefreshTokenAsync("refresh-token");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Network error");
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
