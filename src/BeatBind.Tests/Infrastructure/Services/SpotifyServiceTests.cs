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
    public class SpotifyServiceTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<SpotifyService>> _mockLogger;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly SpotifyService _service;

        public SpotifyServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockLogger = new Mock<ILogger<SpotifyService>>();
            _mockAuthService = new Mock<IAuthenticationService>();

            // Setup default auth behavior
            _mockAuthService.Setup(x => x.GetStoredAuthentication()).Returns((AuthenticationResult?)null);

            _service = new SpotifyService(_httpClient, _mockLogger.Object, _mockAuthService.Object);
        }

        [Fact]
        public void IsAuthenticated_WhenNoAuth_ShouldReturnFalse()
        {
            // Act & Assert
            _service.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task AuthenticateAsync_WhenSuccessful_ShouldReturnTrue()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "valid-token",
                RefreshToken = "refresh-token",
                ExpiresIn = 3600
            };
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(authResult);
            _mockAuthService.Setup(x => x.IsTokenValid(It.IsAny<AuthenticationResult>())).Returns(true);

            // Act
            var result = await _service.AuthenticateAsync();

            // Assert
            result.Should().BeTrue();
            _service.IsAuthenticated.Should().BeTrue();
            _mockAuthService.Verify(x => x.SaveAuthentication(authResult), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenFailed_ShouldReturnFalse()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = false,
                Error = "Authentication failed"
            };
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(authResult);

            // Act
            var result = await _service.AuthenticateAsync();

            // Assert
            result.Should().BeFalse();
            _service.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task PlayAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Put, "https://api.spotify.com/v1/me/player/play");
        }

        [Fact]
        public async Task PauseAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.PauseAsync();

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Put, "https://api.spotify.com/v1/me/player/pause");
        }

        [Fact]
        public async Task NextTrackAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.NextTrackAsync();

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Post, "https://api.spotify.com/v1/me/player/next");
        }

        [Fact]
        public async Task PreviousTrackAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.PreviousTrackAsync();

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Post, "https://api.spotify.com/v1/me/player/previous");
        }

        [Fact]
        public async Task SetVolumeAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetVolumeAsync(75);

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Put, "https://api.spotify.com/v1/me/player/volume?volume_percent=75");
        }

        [Fact]
        public async Task SaveCurrentTrackAsync_WhenAuthenticated_ShouldCallGetPlayback()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupPlaybackResponse();

            // Act
            var result = await _service.SaveCurrentTrackAsync();

            // Assert - Just verify it doesn't throw and attempts to get playback
            // The actual save logic requires the current track ID from playback
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenAuthenticated_ShouldCallGetPlayback()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupPlaybackResponse(shuffleState: false);

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert - Verify it attempts to get playback state
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetCurrentPlaybackAsync_WhenAuthenticated_ShouldMakeRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupPlaybackResponse();

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert - Verify HTTP request was made
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/me/player")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PlayAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task PlayAsync_WhenHttpError_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.InternalServerError);

            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeFalse();
        }

        private async Task SetupAuthenticatedService()
        {
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "valid-token",
                RefreshToken = "refresh-token",
                ExpiresIn = 3600,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ReturnsAsync(authResult);
            _mockAuthService.Setup(x => x.IsTokenValid(It.IsAny<AuthenticationResult>())).Returns(true);
            
            await _service.AuthenticateAsync();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content = "")
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

        private void SetupPlaybackResponse(bool isPlaying = true, bool shuffleState = false)
        {
            var playbackJson = $$"""
            {
                "is_playing": {{(isPlaying ? "true" : "false")}},
                "device": {
                    "id": "device1",
                    "name": "Test Device",
                    "type": "Computer",
                    "is_active": true,
                    "volume_percent": 75
                },
                "shuffle_state": {{(shuffleState ? "true" : "false")}},
                "repeat_state": "off",
                "progress_ms": 10000,
                "item": {
                    "id": "track1",
                    "name": "Test Track",
                    "artists": [{"name": "Test Artist"}],
                    "album": {"name": "Test Album"},
                    "uri": "spotify:track:123",
                    "duration_ms": 180000
                }
            }
            """;

            SetupHttpResponse(HttpStatusCode.OK, playbackJson);
        }

        private void VerifyHttpRequest(HttpMethod method, string url)
        {
            var urlToMatch = url.Split('?')[0];
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains(urlToMatch)),
                ItExpr.IsAny<CancellationToken>());
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
