using System.Net;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

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
        public async Task SetShuffleAsync_WhenAuthenticated_ShouldSendShuffleState()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetShuffleAsync(true);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/me/player/shuffle") &&
                    req.RequestUri.ToString().Contains("state=true")),
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
        public async Task GetCurrentPlaybackAsync_WithEmptyArtistsArray_ShouldReturnPlaybackWithEmptyArtist()
        {
            // Arrange — podcasts and local files can return an empty artists array
            await SetupAuthenticatedService();
            var json = GetPlaybackJson(artistsJson: "[]");
            SetupHttpResponse(HttpStatusCode.OK, json);

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().NotBeNull();
            result!.CurrentTrack.Should().NotBeNull();
            result.CurrentTrack!.Artist.Should().BeEmpty();
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
        public async Task SetShuffleAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Act
            var result = await _service.SetShuffleAsync(true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetShuffleAsync_WhenExceptionThrown_ShouldReturnFalse()
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
            var result = await _service.SetShuffleAsync(true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetRepeatAsync_WhenAuthenticated_ShouldSendRepeatState()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.SetRepeatAsync(RepeatMode.Context);

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/me/player/repeat") &&
                    req.RequestUri.ToString().Contains("state=context")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SetRepeatAsync_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Act
            var result = await _service.SetRepeatAsync(RepeatMode.Off);

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

        [Fact]
        public async Task GetCurrentPlaybackAsync_WithNullItemDuringAd_ShouldReturnStateWithoutTrack()
        {
            // Arrange — "item", "progress_ms", and "volume_percent" are documented as
            // nullable (e.g. during ad breaks or private sessions)
            await SetupAuthenticatedService();
            var json = """
            {
                "is_playing": true,
                "device": {
                    "id": "device1",
                    "name": "Test Device",
                    "type": "Computer",
                    "is_active": true,
                    "is_private_session": false,
                    "is_restricted": false,
                    "volume_percent": null
                },
                "shuffle_state": false,
                "repeat_state": "off",
                "progress_ms": null,
                "item": null
            }
            """;
            SetupHttpResponse(HttpStatusCode.OK, json);

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().NotBeNull();
            result!.IsPlaying.Should().BeTrue();
            result.CurrentTrack.Should().BeNull();
            result.ProgressMs.Should().Be(0);
            result.Volume.Should().BeNull(); // unknown volume must not be treated as 0
        }

        [Fact]
        public async Task Commands_AfterAuthenticationSavedEvent_ShouldUseNewToken()
        {
            // Arrange — a UI re-auth (possibly to a different account) saves new
            // tokens; the singleton service must adopt them immediately
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            var newAuth = new AuthenticationResult
            {
                Success = true,
                AccessToken = "account-b-token",
                RefreshToken = "account-b-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            _mockAuthService.Raise(x => x.AuthenticationSaved += null, _mockAuthService.Object, newAuth);

            // Act
            var result = await _service.PauseAsync();

            // Assert
            result.Should().BeTrue();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Parameter == "account-b-token"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PlayAsync_WhenTokenRejected_ShouldRefreshAndRetryOnce()
        {
            // Arrange — a 401 before the token's expected expiry (revoked token,
            // clock skew) should trigger one forced refresh and one retry
            await SetupAuthenticatedService();
            _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationResult
            {
                Success = true,
                AccessToken = "new-token",
                RefreshToken = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            var sequence = _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });
            sequence.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeTrue();
            _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshFails_ShouldKeepCurrentTokens()
        {
            // Arrange — a transient refresh failure must not wipe the refresh token,
            // otherwise the session is broken until restart
            await SetupAuthenticatedService();
            _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationResult
            {
                Success = false,
                Error = "Service unavailable"
            });

            // Act
            var firstAttempt = await _service.RefreshTokenAsync();

            // A later attempt should still be able to use the original refresh token
            _mockAuthService.Setup(x => x.RefreshTokenAsync("refresh-token")).ReturnsAsync(new AuthenticationResult
            {
                Success = true,
                AccessToken = "new-token",
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            var secondAttempt = await _service.RefreshTokenAsync();

            // Assert
            firstAttempt.Should().BeFalse();
            secondAttempt.Should().BeTrue();
        }

        [Fact]
        public async Task TransferPlaybackAsync_WhenAuthenticated_ShouldSendRequest()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NoContent);

            // Act
            var result = await _service.TransferPlaybackAsync("speaker-1");

            // Assert
            result.Should().BeTrue();
            VerifyHttpRequest(HttpMethod.Put, "https://api.spotify.com/v1/me/player");
        }

        [Fact]
        public async Task TransferPlaybackAsync_WhenApiFails_ShouldReturnFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _service.TransferPlaybackAsync("speaker-1");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TransferPlaybackAsync_WithBlankDeviceId_ShouldNotCallApi()
        {
            // Arrange
            await SetupAuthenticatedService();

            // Act
            var result = await _service.TransferPlaybackAsync("   ");

            // Assert
            result.Should().BeFalse();
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetAvailableDevicesAsync_WhenAuthenticated_ShouldReturnDevices()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.OK, DevicesJson);

            // Act
            var devices = await _service.GetAvailableDevicesAsync();

            // Assert
            devices.Should().HaveCount(2);
            devices[0].Id.Should().Be("laptop-1");
            devices[0].IsActive.Should().BeTrue();
            devices[1].Id.Should().Be("speaker-1");
            devices[1].Name.Should().Be("Living Room");
            devices[1].IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetAvailableDevicesAsync_WhenDeviceOmitsBooleanFields_ShouldStillParseTheList()
        {
            // Arrange - a missing boolean used to throw and discard every device
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.OK, """
                {
                  "devices": [
                    { "id": "speaker-1", "name": "Living Room", "type": "Speaker", "volume_percent": 40 }
                  ]
                }
                """);

            // Act
            var devices = await _service.GetAvailableDevicesAsync();

            // Assert
            devices.Should().ContainSingle();
            devices[0].Id.Should().Be("speaker-1");
            devices[0].IsActive.Should().BeFalse();
            devices[0].VolumePercent.Should().Be(40);
        }

        [Fact]
        public async Task PlayAsync_WhenNoActiveDevice_ShouldTransferToPreferredDevice()
        {
            // Arrange - the favorite is not the active device, so only the preference
            // can explain it being chosen
            await SetupAuthenticatedService();
            var transferBodies = SetupNoActiveDeviceThenTransfer();

            // Act
            var result = await _service.PlayAsync("speaker-1");

            // Assert
            result.Should().BeTrue();
            transferBodies.Should().ContainSingle();
            transferBodies[0].Should().Contain("speaker-1");
        }

        [Fact]
        public async Task PlayAsync_WhenNoActiveDeviceAndNoPreference_ShouldTransferToActiveDevice()
        {
            // Arrange
            await SetupAuthenticatedService();
            var transferBodies = SetupNoActiveDeviceThenTransfer();

            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeTrue();
            transferBodies.Should().ContainSingle();
            transferBodies[0].Should().Contain("laptop-1");
        }

        /// <summary>
        /// Makes the play request 404 ("no active device"), serves the device list, and
        /// records the body of the resulting transfer request.
        /// </summary>
        private List<string> SetupNoActiveDeviceThenTransfer()
        {
            var transferBodies = new List<string>();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken _) =>
                {
                    var url = request.RequestUri!.ToString();

                    if (url.EndsWith("/me/player/play", StringComparison.Ordinal))
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound)
                        {
                            Content = new StringContent(string.Empty)
                        };
                    }

                    if (url.EndsWith("/me/player/devices", StringComparison.Ordinal))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(DevicesJson)
                        };
                    }

                    transferBodies.Add(await request.Content!.ReadAsStringAsync());
                    return new HttpResponseMessage(HttpStatusCode.NoContent)
                    {
                        Content = new StringContent(string.Empty)
                    };
                });

            return transferBodies;
        }

        // The active device is listed first so that picking "speaker-1" can only be
        // the result of an explicit preference.
        private const string DevicesJson = """
            {
              "devices": [
                {
                  "id": "laptop-1",
                  "name": "Laptop",
                  "type": "Computer",
                  "is_active": true,
                  "is_private_session": false,
                  "is_restricted": false,
                  "volume_percent": 55
                },
                {
                  "id": "speaker-1",
                  "name": "Living Room",
                  "type": "Speaker",
                  "is_active": false,
                  "is_private_session": false,
                  "is_restricted": false,
                  "volume_percent": 40
                }
              ]
            }
            """;

        [Fact]
        public async Task PlayAsync_WhenPreferredDeviceIsNotAvailable_ShouldTransferToActiveDevice()
        {
            // Arrange - a stale favorite id: it is no longer among the account's devices
            await SetupAuthenticatedService();
            var transferBodies = SetupNoActiveDeviceThenTransfer();

            // Act
            var result = await _service.PlayAsync("device-that-went-away");

            // Assert
            result.Should().BeTrue();
            transferBodies.Should().ContainSingle();
            transferBodies[0].Should().Contain("laptop-1");
        }

        [Fact]
        public async Task PlayAsync_WhenTransferringToPreferredDevice_ShouldRequestPlayback()
        {
            // Arrange
            await SetupAuthenticatedService();
            var transferBodies = SetupNoActiveDeviceThenTransfer();

            // Act
            await _service.PlayAsync("speaker-1");

            // Assert - the transfer must ask Spotify to start playing, not just move the session
            transferBodies.Should().ContainSingle();
            transferBodies[0].Should().Contain("\"play\":true");
        }

        [Fact]
        public async Task GetAvailableDevicesAsync_WhenBooleanFieldIsJsonNull_ShouldTreatItAsFalse()
        {
            // Arrange
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.OK, """
                {
                  "devices": [
                    { "id": "speaker-1", "name": "Living Room", "type": "Speaker", "is_active": null, "volume_percent": 40 }
                  ]
                }
                """);

            // Act
            var devices = await _service.GetAvailableDevicesAsync();

            // Assert
            devices.Should().ContainSingle();
            devices[0].IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetAvailableDevicesAsync_WhenBooleanFieldHasWrongType_ShouldTreatItAsFalse()
        {
            // Arrange - Spotify should never send this, but a throwing parse would drop every device
            await SetupAuthenticatedService();
            SetupHttpResponse(HttpStatusCode.OK, """
                {
                  "devices": [
                    { "id": "speaker-1", "name": "Living Room", "type": "Speaker", "is_active": "true" }
                  ]
                }
                """);

            // Act
            var devices = await _service.GetAvailableDevicesAsync();

            // Assert
            devices.Should().ContainSingle();
            devices[0].IsActive.Should().BeFalse();
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

        private string GetPlaybackJson(bool isPlaying = true, bool shuffleState = false, string repeatState = "off", string artistsJson = """[{"name": "Test Artist"}]""")
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
                    "artists": {{artistsJson}},
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
