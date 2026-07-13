using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class MusicControlApplicationServiceTests
    {
        private readonly Mock<ISpotifyService> _mockSpotifyService;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<MusicControlApplicationService>> _mockLogger;
        private readonly MusicControlApplicationService _service;

        public MusicControlApplicationServiceTests()
        {
            _mockSpotifyService = new Mock<ISpotifyService>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<MusicControlApplicationService>>();
            _service = new MusicControlApplicationService(_mockSpotifyService.Object, _mockConfigService.Object, _mockLogger.Object);
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
        public async Task PlayAsync_ShouldCallSpotifyService()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.PlayAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PlayAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PlayAsync(), Times.Once);
        }

        [Fact]
        public async Task PauseAsync_ShouldCallSpotifyService()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.PauseAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PauseAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PauseAsync(), Times.Once);
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
        public async Task ToggleMuteAsync_WhenNotMuted_ShouldMute()
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
            var result = await _service.ToggleMuteAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(0), Times.Once);
        }

        [Fact]
        public async Task ToggleMuteAsync_WhenMuted_ShouldUnmute()
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
            await _service.ToggleMuteAsync(); // Store the volume

            // Arrange - Then unmute
            var playbackState2 = new PlaybackState { IsPlaying = true, Volume = 0, ProgressMs = 2000, DurationMs = 200000 };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState2);

            // Act
            var result = await _service.ToggleMuteAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(50), Times.Once);
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
        public async Task MuteAsync_WhenAlreadyMuted_ShouldDoNothing()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 0,
                ProgressMs = 1000,
                DurationMs = 200000,
                ShuffleState = false,
                RepeatState = RepeatMode.Off,
                CurrentTrack = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" }
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);

            // Act
            var result = await _service.MuteAsync();

            // Assert
            result.Should().BeFalse();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UnmuteAsync_WhenMuted_ShouldUnmute()
        {
            // Arrange - First mute to set last volume
            var playbackState1 = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ProgressMs = 1000,
                DurationMs = 200000
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState1);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(It.IsAny<int>())).ReturnsAsync(true);
            await _service.ToggleMuteAsync(); // Store the volume

            // Arrange - Then unmute
            var playbackState2 = new PlaybackState { IsPlaying = true, Volume = 0, ProgressMs = 2000, DurationMs = 200000 };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState2);

            // Act
            var result = await _service.UnmuteAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(50), Times.Once);
        }

        [Fact]
        public async Task UnmuteAsync_WhenNotMuted_ShouldDoNothing()
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

            // Act
            var result = await _service.UnmuteAsync();

            // Assert
            result.Should().BeFalse();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenShuffleOff_ShouldEnableShuffle()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ShuffleState = false,
                ProgressMs = 1000,
                DurationMs = 200000
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetShuffleAsync(true)).ReturnsAsync(true);

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetShuffleAsync(true), Times.Once);
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenPressedTwiceRapidly_ShouldToggleBothWays()
        {
            // Arrange — the second press within the cache window must see the shuffle
            // state left by the first press
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                ShuffleState = false,
                ProgressMs = 1000,
                DurationMs = 200000
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetShuffleAsync(It.IsAny<bool>())).ReturnsAsync(true);

            // Act
            var first = await _service.ToggleShuffleAsync();
            var second = await _service.ToggleShuffleAsync();

            // Assert
            first.Should().BeTrue();
            second.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.GetCurrentPlaybackAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.SetShuffleAsync(true), Times.Once);
            _mockSpotifyService.Verify(x => x.SetShuffleAsync(false), Times.Once);
        }

        [Fact]
        public async Task ToggleRepeatAsync_WhenRepeatOff_ShouldSetToContext()
        {
            // Arrange
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 50,
                RepeatState = RepeatMode.Off,
                ProgressMs = 1000,
                DurationMs = 200000
            };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetRepeatAsync(RepeatMode.Context)).ReturnsAsync(true);

            // Act
            var result = await _service.ToggleRepeatAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SetRepeatAsync(RepeatMode.Context), Times.Once);
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

        [Fact]
        public async Task PlayPauseAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.PlayPauseAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task NextTrackAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.NextTrackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.NextTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task PreviousTrackAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            var config = new ApplicationConfiguration { PreviousTrackRewindToStart = false };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.PreviousTrackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.PreviousTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task VolumeUpAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetConfiguration()).Throws(new Exception("Config error"));

            // Act
            var result = await _service.VolumeUpAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task VolumeDownAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetConfiguration()).Throws(new Exception("Config error"));

            // Act
            var result = await _service.VolumeDownAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task MuteAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.MuteAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SaveTrackAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.SaveCurrentTrackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.SaveTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveTrackAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.RemoveCurrentTrackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.RemoveTrackAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleShuffleAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.ToggleShuffleAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleRepeatAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.ToggleRepeatAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekForwardAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetConfiguration()).Throws(new Exception("Config error"));

            // Act
            var result = await _service.SeekForwardAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekBackwardAsync_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetConfiguration()).Throws(new Exception("Config error"));

            // Act
            var result = await _service.SeekBackwardAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCurrentPlaybackAsync_WhenExceptionThrown_ShouldReturnNull()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _service.GetCurrentPlaybackAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task VolumeUpAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.VolumeUpAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task VolumeDownAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.VolumeDownAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task MuteAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.MuteAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekForwardAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            var config = new ApplicationConfiguration { SeekMilliseconds = 5000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.SeekForwardAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekBackwardAsync_WhenNoPlaybackState_ShouldReturnFalse()
        {
            // Arrange
            var config = new ApplicationConfiguration { SeekMilliseconds = 5000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);

            // Act
            var result = await _service.SeekBackwardAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SeekForwardAsync_WhenPositionExceedsDuration_ShouldSeekToEnd()
        {
            // Arrange
            var config = new ApplicationConfiguration { SeekMilliseconds = 10000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            var playbackState = new PlaybackState { ProgressMs = 195000, DurationMs = 200000, IsPlaying = true };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SeekToPositionAsync(200000)).ReturnsAsync(true);

            // Act
            var result = await _service.SeekForwardAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SeekToPositionAsync(200000), Times.Once);
        }

        [Fact]
        public async Task SeekBackwardAsync_WhenPositionGoesNegative_ShouldSeekToZero()
        {
            // Arrange
            var config = new ApplicationConfiguration { SeekMilliseconds = 10000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            var playbackState = new PlaybackState { ProgressMs = 5000, DurationMs = 200000, IsPlaying = true };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SeekToPositionAsync(0)).ReturnsAsync(true);

            // Act
            var result = await _service.SeekBackwardAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.SeekToPositionAsync(0), Times.Once);
        }

        [Fact]
        public async Task VolumeUpAsync_WhenDeviceVolumeUnknown_ShouldDoNothing()
        {
            // Arrange — Spotify reports volume_percent: null for some devices and
            // during ads; stepping from an assumed level would yank the real volume
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            var playbackState = new PlaybackState { IsPlaying = true, Volume = null, ProgressMs = 1000, DurationMs = 200000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);

            // Act
            var result = await _service.VolumeUpAsync();

            // Assert
            result.Should().BeFalse();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleMuteAsync_WhenDeviceVolumeUnknown_ShouldDoNothing()
        {
            // Arrange — an unknown volume must not be treated as muted
            var playbackState = new PlaybackState { IsPlaying = true, Volume = null, ProgressMs = 1000, DurationMs = 200000 };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);

            // Act
            var result = await _service.ToggleMuteAsync();

            // Assert
            result.Should().BeFalse();
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PlayPauseAsync_WhenCachedStateIsStale_ShouldRefetchAndRetry()
        {
            // Arrange — the cached state says playing, but playback ended on its
            // own, so the pause is rejected; the service must refetch and retry
            var staleState = new PlaybackState { IsPlaying = true, Volume = 50, ProgressMs = 1000, DurationMs = 200000 };
            var freshState = new PlaybackState { IsPlaying = false, Volume = 50, ProgressMs = 0, DurationMs = 200000 };
            _mockSpotifyService.SetupSequence(x => x.GetCurrentPlaybackAsync())
                .ReturnsAsync(staleState)
                .ReturnsAsync(freshState);
            _mockSpotifyService.Setup(x => x.PauseAsync()).ReturnsAsync(false);
            _mockSpotifyService.Setup(x => x.PlayAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PlayPauseAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PauseAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.PlayAsync(), Times.Once);
        }

        [Fact]
        public async Task VolumeUpAsync_WhenPressedRapidly_ShouldReuseCachedPlaybackState()
        {
            // Arrange — presses within the cache window should fetch the playback
            // state once and compute successive volume steps locally
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            var playbackState = new PlaybackState { IsPlaying = true, Volume = 50, ProgressMs = 1000, DurationMs = 200000 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(It.IsAny<int>())).ReturnsAsync(true);

            // Act
            var first = await _service.VolumeUpAsync();
            var second = await _service.VolumeUpAsync();

            // Assert
            first.Should().BeTrue();
            second.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.GetCurrentPlaybackAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(60), Times.Once);
            _mockSpotifyService.Verify(x => x.SetVolumeAsync(70), Times.Once);
        }

        [Fact]
        public async Task NextTrackAsync_ShouldInvalidateCachedPlaybackState()
        {
            // Arrange
            var config = new ApplicationConfiguration { VolumeSteps = 10 };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync())
                .ReturnsAsync(() => new PlaybackState { IsPlaying = true, Volume = 50, ProgressMs = 1000, DurationMs = 200000 });
            _mockSpotifyService.Setup(x => x.SetVolumeAsync(It.IsAny<int>())).ReturnsAsync(true);
            _mockSpotifyService.Setup(x => x.NextTrackAsync()).ReturnsAsync(true);

            // Act — the skip changes the track, so the cached state must be discarded
            await _service.VolumeUpAsync();
            await _service.NextTrackAsync();
            await _service.VolumeUpAsync();

            // Assert
            _mockSpotifyService.Verify(x => x.GetCurrentPlaybackAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task PlayPauseAsync_WhenPressedTwiceRapidly_ShouldToggleBothWays()
        {
            // Arrange — the second press within the cache window must see the state
            // left by the first press, not the original snapshot
            var playbackState = new PlaybackState { IsPlaying = true, Volume = 50, ProgressMs = 1000, DurationMs = 200000 };
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync(playbackState);
            _mockSpotifyService.Setup(x => x.PauseAsync()).ReturnsAsync(true);
            _mockSpotifyService.Setup(x => x.PlayAsync()).ReturnsAsync(true);

            // Act
            var first = await _service.PlayPauseAsync();
            var second = await _service.PlayPauseAsync();

            // Assert
            first.Should().BeTrue();
            second.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PauseAsync(), Times.Once);
            _mockSpotifyService.Verify(x => x.PlayAsync(), Times.Once);
        }

        [Fact]
        public async Task PreviousTrackAsync_WhenRewindToStartAndNoPlaybackState_ShouldGoToPreviousTrack()
        {
            // Arrange
            var config = new ApplicationConfiguration { PreviousTrackRewindToStart = true };
            _mockConfigService.Setup(x => x.GetConfiguration()).Returns(config);
            _mockSpotifyService.Setup(x => x.GetCurrentPlaybackAsync()).ReturnsAsync((PlaybackState?)null);
            _mockSpotifyService.Setup(x => x.PreviousTrackAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.PreviousTrackAsync();

            // Assert
            result.Should().BeTrue();
            _mockSpotifyService.Verify(x => x.PreviousTrackAsync(), Times.Once);
        }
    }
}
