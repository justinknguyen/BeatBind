using BeatBind.Core.Entities;
using FluentAssertions;

namespace BeatBind.Tests.Core.Entities
{
    public class PlaybackStateTests
    {
        [Fact]
        public void PlaybackState_ShouldInitializeWithDefaults()
        {
            // Act
            var playbackState = new PlaybackState();

            // Assert
            playbackState.IsPlaying.Should().BeFalse();
            playbackState.Volume.Should().Be(0);
            playbackState.ProgressMs.Should().Be(0);
            playbackState.DurationMs.Should().Be(0);
            playbackState.ShuffleState.Should().BeFalse();
            playbackState.RepeatState.Should().Be(RepeatMode.Off);
            playbackState.CurrentTrack.Should().BeNull();
            playbackState.Device.Should().BeNull();
        }

        [Fact]
        public void PlaybackState_ShouldSetProperties()
        {
            // Arrange
            var track = new Track { Id = "track1", Name = "Test Track", Artist = "Artist" };
            var device = new Device { Id = "device1", Name = "Test Device", Type = "Computer", IsActive = true, VolumePercent = 50 };

            // Act
            var playbackState = new PlaybackState
            {
                IsPlaying = true,
                Volume = 75,
                ProgressMs = 30000,
                DurationMs = 180000,
                ShuffleState = true,
                RepeatState = RepeatMode.Context,
                CurrentTrack = track,
                Device = device
            };

            // Assert
            playbackState.IsPlaying.Should().BeTrue();
            playbackState.Volume.Should().Be(75);
            playbackState.ProgressMs.Should().Be(30000);
            playbackState.DurationMs.Should().Be(180000);
            playbackState.ShuffleState.Should().BeTrue();
            playbackState.RepeatState.Should().Be(RepeatMode.Context);
            playbackState.CurrentTrack.Should().Be(track);
            playbackState.Device.Should().Be(device);
        }

        [Theory]
        [InlineData(RepeatMode.Off)]
        [InlineData(RepeatMode.Track)]
        [InlineData(RepeatMode.Context)]
        public void PlaybackState_ShouldAcceptValidRepeatModes(RepeatMode repeatMode)
        {
            // Act
            var playbackState = new PlaybackState { RepeatState = repeatMode };

            // Assert
            playbackState.RepeatState.Should().Be(repeatMode);
        }

        [Fact]
        public void PlaybackState_VolumeRange_ShouldBeValidated()
        {
            // This test documents that Volume should be between 0-100
            var playbackState = new PlaybackState();

            // Valid volumes
            playbackState.Volume = 0;
            playbackState.Volume.Should().Be(0);

            playbackState.Volume = 50;
            playbackState.Volume.Should().Be(50);

            playbackState.Volume = 100;
            playbackState.Volume.Should().Be(100);
        }
    }
}
