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

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnTrue()
        {
            // Arrange
            var authResult = new AuthenticationResult
            {
                Success = true,
                AccessToken = "valid-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            await SetupAuthenticatedService();
            _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationResult
            {
                Success = true,
                AccessToken = "new-token",
                RefreshToken = "refresh-token"
            });
            _mockAuthService.Setup(x => x.IsTokenValid(It.IsAny<AuthenticationResult>())).Returns(true);

            // Act
            var result = await _service.RefreshTokenAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenNoAuth_ShouldReturnFalse()
        {
            // Act
            var result = await _service.RefreshTokenAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Network error"));

            // Act
            var result = await _service.RefreshTokenAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetVolumeAsync_WhenAuthenticatedAndValid_ShouldSendVolumeRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetVolumeAsync(75);

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Put, "https://api.spotify.com/v1/me/player/volume");
        }

        [Fact]
        public async Task SetVolumeAsync_WithVolumeTooHigh_ShouldClampTo100()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetVolumeAsync(150);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("volume_percent=100")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SetVolumeAsync_WithVolumeTooLow_ShouldClampTo0()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetVolumeAsync(-10);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("volume_percent=0")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SetVolumeAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Act
            var result = await _service.SetVolumeAsync(50);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetVolumeAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.SetVolumeAsync(50);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCurrentPlaybackAsync_WhenNoActiveDevice_ShouldReturnNull()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentPlaybackAsync_WhenExceptionThrown_ShouldReturnNull()
        {
            // Arrange
            await SetupAuthenticatedService();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AuthenticateAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockAuthService.Setup(x => x.AuthenticateAsync()).ThrowsAsync(new Exception("Auth error"));

            // Act
            var result = await _service.AuthenticateAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenPlaybackAvailable_ShouldToggle()
        {
            // Arrange
            await SetupAuthenticatedService();
            var sequence = _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(GetPlaybackJson(true, false))
            });
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenNoPlayback_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleRepeatAsync_WhenRepeatOff_ShouldSetToContext()
        {
            // Arrange
            await SetupAuthenticatedService();
            var sequence = _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(GetPlaybackJson(true, false, "off"))
            });
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            // Act
            var result = await _service.ToggleRepeatAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ToggleRepeatAsync_WhenNoPlayback_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.ToggleRepeatAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SaveCurrentTrackAsync_WhenTrackAvailable_ShouldSaveTrack()
        {
            // Arrange
            await SetupAuthenticatedService();
            var sequence = _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(GetPlaybackJson(true, false))
            });
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _service.SaveCurrentTrackAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task SaveCurrentTrackAsync_WhenNoPlayback_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SaveCurrentTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveCurrentTrackAsync_WhenTrackAvailable_ShouldRemoveTrack()
        {
            // Arrange
            await SetupAuthenticatedService();
            var sequence = _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(GetPlaybackJson(true, false))
            });
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _service.RemoveCurrentTrackAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveCurrentTrackAsync_WhenNoPlayback_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.RemoveCurrentTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekToPositionAsync_WhenAuthenticated_ShouldSeekToPosition()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SeekToPositionAsync(30000);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("seek") &&
                    req.RequestUri.ToString().Contains("position_ms=30000")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SeekToPositionAsync_WithNegativePosition_ShouldClampToZero()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SeekToPositionAsync(-5000);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("position_ms=0")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SeekToPositionAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Act
            var result = await _service.SeekToPositionAsync(30000);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekToPositionAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.SeekToPositionAsync(30000);

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
            var playbackJson = GetPlaybackJson(isPlaying, shuffleState);
            SetupHttpResponse(HttpStatusCode.OK, playbackJson);
        }

        private string GetPlaybackJson(bool isPlaying = true, bool shuffleState = false, string repeatState = "off")
        {
            return $$"""
            {
                "is_playing": {{(isPlaying ? "true" : "false")}},
                "device": {
                    "id": "device1",
                    "name": "Test Device",
                    "type": "Computer",
                    "is_active": true,
                    "is_private_session": false,
                    "is_restricted": false,
                    "volume_percent": 75
                },
                "shuffle_state": {{(shuffleState ? "true" : "false")}},
                "repeat_state": "{{repeatState}}",
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
