using BeatBind.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BeatBind.Tests.Domain.Entities
{
    public class HotkeyTests
    {
        [Fact]
        public void Hotkey_ShouldInitializeWithCorrectValues()
        {
            // Arrange & Act
            var hotkey = new Hotkey
            {
                Id = 1,
                Action = HotkeyAction.PlayPause,
                KeyCode = 32, // Space key
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift
            };

            // Assert
            hotkey.Id.Should().Be(1);
            hotkey.Action.Should().Be(HotkeyAction.PlayPause);
            hotkey.KeyCode.Should().Be(32);
            hotkey.Modifiers.Should().Be(ModifierKeys.Control | ModifierKeys.Shift);
        }

        [Fact]
        public void Hotkey_ShouldAllowNoModifiers()
        {
            // Arrange & Act
            var hotkey = new Hotkey
            {
                Id = 1,
                Action = HotkeyAction.NextTrack,
                KeyCode = 78, // N key
                Modifiers = ModifierKeys.None
            };

            // Assert
            hotkey.Modifiers.Should().Be(ModifierKeys.None);
        }

        [Fact]
        public void Hotkey_ShouldDefaultToEnabled()
        {
            // Arrange & Act
            var hotkey = new Hotkey();

            // Assert
            hotkey.IsEnabled.Should().BeTrue();
        }
    }

    public class ApplicationConfigurationTests
    {
        [Fact]
        public void ApplicationConfiguration_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new ApplicationConfiguration();

            // Assert
            config.ClientId.Should().BeEmpty();
            config.ClientSecret.Should().BeEmpty();
            config.RedirectUri.Should().Be("http://127.0.0.1:8888/callback");
            config.StartMinimized.Should().BeFalse();
            config.MinimizeToTray.Should().BeTrue();
            config.DarkMode.Should().BeFalse();
            config.VolumeSteps.Should().Be(10);
            config.SeekMilliseconds.Should().Be(10000);
            config.Hotkeys.Should().NotBeNull();
        }

        [Fact]
        public void ApplicationConfiguration_ShouldAllowPropertyUpdates()
        {
            // Arrange
            var config = new ApplicationConfiguration();

            // Act
            config.ClientId = "test-id";
            config.ClientSecret = "test-secret";
            config.DarkMode = false;
            config.VolumeSteps = 10;

            // Assert
            config.ClientId.Should().Be("test-id");
            config.ClientSecret.Should().Be("test-secret");
            config.DarkMode.Should().BeFalse();
            config.VolumeSteps.Should().Be(10);
        }
    }

    public class TrackTests
    {
        [Fact]
        public void Track_ShouldInitializeWithCorrectValues()
        {
            // Arrange & Act
            var track = new Track
            {
                Id = "track-123",
                Name = "Test Song",
                Artist = "Test Artist",
                Album = "Test Album",
                DurationMs = 180000,
                IsPlaying = true
            };

            // Assert
            track.Id.Should().Be("track-123");
            track.Name.Should().Be("Test Song");
            track.Artist.Should().Be("Test Artist");
            track.Album.Should().Be("Test Album");
            track.DurationMs.Should().Be(180000);
            track.IsPlaying.Should().BeTrue();
        }
    }

    public class DeviceTests
    {
        [Fact]
        public void Device_ShouldInitializeWithCorrectValues()
        {
            // Arrange & Act
            var device = new Device
            {
                Id = "device-123",
                Name = "My Computer",
                Type = "Computer",
                IsActive = true,
                VolumePercent = 75
            };

            // Assert
            device.Id.Should().Be("device-123");
            device.Name.Should().Be("My Computer");
            device.Type.Should().Be("Computer");
            device.IsActive.Should().BeTrue();
            device.VolumePercent.Should().Be(75);
        }
    }
}
