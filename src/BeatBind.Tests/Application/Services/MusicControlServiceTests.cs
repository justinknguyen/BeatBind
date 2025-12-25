using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class MusicControlServiceTests
    {
        private readonly Mock<ISpotifyService> _mockSpotifyService;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<MusicControlService>> _mockLogger;
        private readonly MusicControlService _service;

        public MusicControlServiceTests()
        {
            _mockSpotifyService = new Mock<ISpotifyService>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<MusicControlService>>();
            _service = new MusicControlService(_mockSpotifyService.Object, _mockConfigService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task PlayPauseAsync_WhenPlaying_ShouldPause()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.PauseAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PlayPauseAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PauseAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.PlayAsync(), Times.Never);
        }

        [Fact]
        public async Task PlayPauseAsync_WhenPaused_ShouldPlay()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = false,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.PlayAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PlayPauseAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PlayAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.PauseAsync(), Times.Never);
        }

        [Fact]
        public async Task PlayPauseAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.PlayPauseAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task NextTrackAsync_ShouldCallSpotifyService()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.NextTrackAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.NextTrackAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.NextTrackAsync(), Times.Once);
        }

        [Fact]
        public async Task PreviousTrackAsync_WhenProgressOver5Seconds_ShouldRewindToStart()
        {
            // Arrange
            var config = new ApplicationConfiguration { PreviousTrackRewindToStart = true };
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 10000, // 10 seconds
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SeekToPositionAsync(0)).ReturnsAsync(true);

            // Act
            var result = await _service.PreviousTrackAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SeekToPositionAsync(0), Times.Once);
            _mockSpotifyService.Verify(x => x.PreviousTrackAsync(), Times.Never);
        }

        [Fact]
        public async Task PreviousTrackAsync_WhenProgressUnder5Seconds_ShouldSkipToPrevious()
        {
            // Arrange
            var config = new ApplicationConfiguration { PreviousTrackRewindToStart = true };
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 3000, // 3 seconds
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.PreviousTrackAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PreviousTrackAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PreviousTrackAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.SeekToPositionAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task VolumeUpAsync_ShouldIncreaseVolume()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(60)).ReturnsAsync(true);

            // Act
            var result = await _service.VolumeUpAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(60), Times.Once);
        }

        [Fact]
        public async Task VolumeUpAsync_WhenAtMax_ShouldCapAt100()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 95,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(100)).ReturnsAsync(true);

            // Act
            var result = await _service.VolumeUpAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(100), Times.Once);
        }

        [Fact]
        public async Task VolumeDownAsync_ShouldDecreaseVolume()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(40)).ReturnsAsync(true);

            // Act
            var result = await _service.VolumeDownAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(40), Times.Once);
        }

        [Fact]
        public async Task MuteAsync_WhenNotMuted_ShouldMute()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(0)).ReturnsAsync(true);

            // Act
            var result = await _service.MuteAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(0), Times.Once);
        }

        [Fact]
        public async Task MuteAsync_WhenMuted_ShouldUnmute()
        {
            // Arrange - First mute
            var playbackState1 = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState1);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(It.IsAny<int>())).ReturnsAsync(true);
            await _service.MuteAsync(); // Store the volume

            // Arrange - Then unmute
            var playbackState2 = new PlaybackState { IsPlaying = true, Volume = 0, ProgressMs = 2000, DurationMs = 200000 };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState2);

            // Act
            var result = await _service.MuteAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(50), Times.Once);
        }

        [Fact]
        public async Task ToggleShuffleAsync_ShouldCallSpotifyService()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.ToggleShuffleAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.ToggleShuffleAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveTrackAsync_ShouldCallSpotifyService()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.SaveCurrentTrackAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.SaveTrackAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SaveCurrentTrackAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentPlaybackAsync_ShouldReturnPlaybackState()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 75,
                CurrentTrack = new Track { Name = "Test Song", Artist = "Test Artist" }
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().NotBeNull();
            result!.IsPlaying.Should().BeTrue();
            result.Volume.Should().Be(75);
        }
    }
}
