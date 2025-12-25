using BeatBind.Core.Entities;
using FluentAssertions;

namespace BeatBind.Tests.Core.Entities
{
    public class DeviceTests2
    {
        [Fact]
        public void Device_ShouldSetAllProperties()
        {
            // Act
            var device = new Device
            {
                Id = "device-123",
                Name = "My Computer",
                Type = "Computer",
                IsActive = true,
                VolumePercent = 85
            };

            // Assert
            device.Id.Should().Be("device-123");
            device.Name.Should().Be("My Computer");
            device.Type.Should().Be("Computer");
            device.IsActive.Should().BeTrue();
            device.VolumePercent.Should().Be(85);
        }

        [Theory]
        [InlineData("Computer")]
        [InlineData("Smartphone")]
        [InlineData("Speaker")]
        [InlineData("TV")]
        public void Device_ShouldAcceptDifferentDeviceTypes(string deviceType)
        {
            // Act
            var device = new Device { Type = deviceType };

            // Assert
            device.Type.Should().Be(deviceType);
        }

        [Fact]
        public void Device_VolumePercent_ShouldAcceptValidRange()
        {
            // Arrange & Act
            var device1 = new Device { VolumePercent = 0 };
            var device2 = new Device { VolumePercent = 50 };
            var device3 = new Device { VolumePercent = 100 };

            // Assert
            device1.VolumePercent.Should().Be(0);
            device2.VolumePercent.Should().Be(50);
            device3.VolumePercent.Should().Be(100);
        }
    }

    public class TrackTests2
    {
        [Fact]
        public void Track_ShouldCalculateProgressPercentage()
        {
            // Arrange
            var track = new Track
            {
                ProgressMs = 30000,  // 30 seconds
                DurationMs = 180000  // 3 minutes
            };

            // Act
            var progressPercent = (double)track.ProgressMs / track.DurationMs * 100;

            // Assert
            progressPercent.Should().BeApproximately(16.67, 0.01);
        }

        [Fact]
        public void Track_WithLongDuration_ShouldStoreCorrectly()
        {
            // Arrange & Act
            var track = new Track
            {
                Id = "long-track",
                Name = "Epic Song",
                DurationMs = 600000  // 10 minutes
            };

            // Assert
            track.DurationMs.Should().Be(600000);
        }

        [Fact]
        public void Track_WithUriFormat_ShouldBeValid()
        {
            // Arrange & Act
            var track = new Track
            {
                Uri = "spotify:track:3n3Ppam7vgaVa1iaRUc9Lp"
            };

            // Assert
            track.Uri.Should().StartWith("spotify:track:");
            track.Uri.Should().HaveLength(36);
        }
    }

    public class ApplicationConfigurationTests2
    {
        [Fact]
        public void ApplicationConfiguration_ShouldSetAllMusicControlSettings()
        {
            // Act
            var config = new ApplicationConfiguration
            {
                VolumeSteps = 5,
                SeekMilliseconds = 15000,
                PreviousTrackRewindToStart = false
            };

            // Assert
            config.VolumeSteps.Should().Be(5);
            config.SeekMilliseconds.Should().Be(15000);
            config.PreviousTrackRewindToStart.Should().BeFalse();
        }

        [Fact]
        public void ApplicationConfiguration_ShouldManageHotkeysCollection()
        {
            // Arrange
            var config = new ApplicationConfiguration();
            var hotkey1 = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, KeyCode = 0xB3 };
            var hotkey2 = new Hotkey { Id = 2, Action = HotkeyAction.NextTrack, KeyCode = 0xB0 };

            // Act
            config.Hotkeys.Add(hotkey1);
            config.Hotkeys.Add(hotkey2);

            // Assert
            config.Hotkeys.Should().HaveCount(2);
            config.Hotkeys.Should().Contain(h => h.Action == HotkeyAction.PlayPause);
            config.Hotkeys.Should().Contain(h => h.Action == HotkeyAction.NextTrack);
        }

        [Fact]
        public void ApplicationConfiguration_WithCustomRedirectUri_ShouldStore()
        {
            // Act
            var config = new ApplicationConfiguration
            {
                RedirectUri = "http://localhost:5000/callback"
            };

            // Assert
            config.RedirectUri.Should().Be("http://localhost:5000/callback");
        }
    }

    public class HotkeyTests2
    {
        [Theory]
        [InlineData(HotkeyAction.PlayPause)]
        [InlineData(HotkeyAction.NextTrack)]
        [InlineData(HotkeyAction.PreviousTrack)]
        [InlineData(HotkeyAction.VolumeUp)]
        [InlineData(HotkeyAction.VolumeDown)]
        [InlineData(HotkeyAction.Mute)]
        [InlineData(HotkeyAction.SaveTrack)]
        [InlineData(HotkeyAction.RemoveTrack)]
        [InlineData(HotkeyAction.ToggleShuffle)]
        [InlineData(HotkeyAction.ToggleRepeat)]
        [InlineData(HotkeyAction.SeekForward)]
        [InlineData(HotkeyAction.SeekBackward)]
        public void Hotkey_ShouldSupportAllActions(HotkeyAction action)
        {
            // Act
            var hotkey = new Hotkey { Action = action };

            // Assert
            hotkey.Action.Should().Be(action);
        }

        [Fact]
        public void Hotkey_WithMultipleModifiers_ShouldCombine()
        {
            // Act
            var hotkey = new Hotkey
            {
                KeyCode = 0x41, // 'A' key
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift
            };

            // Assert
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Control);
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Shift);
            hotkey.Modifiers.Should().NotHaveFlag(ModifierKeys.Alt);
        }

        [Fact]
        public void Hotkey_WithAllModifiers_ShouldCombine()
        {
            // Act
            var hotkey = new Hotkey
            {
                Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Windows
            };

            // Assert
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Control);
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Alt);
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Shift);
            hotkey.Modifiers.Should().HaveFlag(ModifierKeys.Windows);
        }

        [Fact]
        public void Hotkey_MediaKeys_ShouldUseCorrectKeyCodes()
        {
            // Arrange - Windows media key codes
            var mediaPlayPause = 0xB3;
            var mediaNextTrack = 0xB0;
            var mediaPreviousTrack = 0xB1;
            var mediaVolumeUp = 0xAF;
            var mediaVolumeDown = 0xAE;

            // Act & Assert
            var hotkeys = new[]
            {
                new Hotkey { KeyCode = mediaPlayPause, Action = HotkeyAction.PlayPause },
                new Hotkey { KeyCode = mediaNextTrack, Action = HotkeyAction.NextTrack },
                new Hotkey { KeyCode = mediaPreviousTrack, Action = HotkeyAction.PreviousTrack },
                new Hotkey { KeyCode = mediaVolumeUp, Action = HotkeyAction.VolumeUp },
                new Hotkey { KeyCode = mediaVolumeDown, Action = HotkeyAction.VolumeDown }
            };

            hotkeys.Should().HaveCount(5);
            hotkeys.Should().OnlyContain(h => h.KeyCode > 0);
        }
    }

    public class ResultPatternTests
    {
        [Fact]
        public void Result_WithValue_ShouldProvideAccess()
        {
            // Arrange
            var expectedValue = 42;

            // Act
            var result = BeatBind.Core.Common.Result.Success(expectedValue);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void Result_Failure_ShouldContainError()
        {
            // Arrange
            var errorMessage = "Something went wrong";

            // Act
            var result = BeatBind.Core.Common.Result.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(errorMessage);
        }

        [Fact]
        public void Result_GenericFailure_ShouldHaveDefaultValue()
        {
            // Act
            var result = BeatBind.Core.Common.Result.Failure<int>("Error");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Value.Should().Be(default(int));
        }

        [Fact]
        public void Result_Success_ShouldNotHaveError()
        {
            // Act
            var result = BeatBind.Core.Common.Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeEmpty();
        }
    }
}
