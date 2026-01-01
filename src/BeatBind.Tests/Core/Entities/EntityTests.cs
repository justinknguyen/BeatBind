using BeatBind.Core.Entities;
using FluentAssertions;

namespace BeatBind.Tests.Core.Entities
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

        [Theory]
        [InlineData(HotkeyAction.PlayPause, "Play/Pause")]
        [InlineData(HotkeyAction.NextTrack, "Next Track")]
        [InlineData(HotkeyAction.PreviousTrack, "Previous Track")]
        [InlineData(HotkeyAction.VolumeUp, "Volume Up")]
        [InlineData(HotkeyAction.VolumeDown, "Volume Down")]
        [InlineData(HotkeyAction.MuteUnmute, "Mute/Unmute")]
        [InlineData(HotkeyAction.SeekForward, "Seek Forward")]
        [InlineData(HotkeyAction.SeekBackward, "Seek Backward")]
        [InlineData(HotkeyAction.SaveTrack, "Save Track")]
        [InlineData(HotkeyAction.RemoveTrack, "Remove Track")]
        [InlineData(HotkeyAction.ToggleShuffle, "Toggle Shuffle")]
        [InlineData(HotkeyAction.ToggleRepeat, "Toggle Repeat")]
        public void GetActionDisplayName_ShouldReturnCorrectDisplayName(HotkeyAction action, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetActionDisplayName(action);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(48, "0")]
        [InlineData(49, "1")]
        [InlineData(57, "9")]
        [InlineData(65, "A")]
        [InlineData(90, "Z")]
        [InlineData(112, "F1")]
        [InlineData(123, "F12")]
        [InlineData(32, "Space")]
        [InlineData(13, "Enter")]
        [InlineData(9, "Tab")]
        [InlineData(27, "Escape")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForCommonKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(96, "Numpad 0")]
        [InlineData(105, "Numpad 9")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForNumpadKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(179, "Media Play/Pause")]
        [InlineData(176, "Media Next Track")]
        [InlineData(177, "Media Previous Track")]
        [InlineData(178, "Media Stop")]
        [InlineData(173, "Volume Mute")]
        [InlineData(175, "Volume Up")]
        [InlineData(174, "Volume Down")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForMediaKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(37, "← (Left Arrow)")]
        [InlineData(38, "↑ (Up Arrow)")]
        [InlineData(39, "→ (Right Arrow)")]
        [InlineData(40, "↓ (Down Arrow)")]
        [InlineData(33, "Page Up")]
        [InlineData(34, "Page Down")]
        [InlineData(36, "Home")]
        [InlineData(35, "End")]
        [InlineData(45, "Insert")]
        [InlineData(46, "Delete")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForNavigationKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(186, "; (Semicolon / Ö / Ä)")]
        [InlineData(187, "= (Plus)")]
        [InlineData(188, ", (Comma)")]
        [InlineData(189, "- (Minus)")]
        [InlineData(190, ". (Period)")]
        [InlineData(191, "/ (Slash)")]
        [InlineData(192, "` (Tilde / Ø)")]
        [InlineData(219, "[ (Open Bracket / Å)")]
        [InlineData(220, "\\ (Backslash)")]
        [InlineData(221, "] (Close Bracket)")]
        [InlineData(222, "' (Quote / Æ)")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForPunctuationKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Theory]
        [InlineData(166, "Browser Back")]
        [InlineData(167, "Browser Forward")]
        [InlineData(168, "Browser Refresh")]
        [InlineData(169, "Browser Stop")]
        [InlineData(170, "Browser Search")]
        [InlineData(171, "Browser Favorites")]
        [InlineData(172, "Browser Home")]
        public void GetKeyDisplayName_ShouldReturnCorrectName_ForBrowserKeys(int keyCode, string expectedName)
        {
            // Act
            var displayName = Hotkey.GetKeyDisplayName(keyCode);

            // Assert
            displayName.Should().Be(expectedName);
        }

        [Fact]
        public void GetKeyDisplayName_ShouldReturnGenericName_ForUnknownKey()
        {
            // Arrange
            int unknownKeyCode = 999;

            // Act
            var displayName = Hotkey.GetKeyDisplayName(unknownKeyCode);

            // Assert
            displayName.Should().Be("Key999");
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainFunctionKeys()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(112); // F1
            Hotkey.AvailableKeyCodes.Should().Contain(123); // F12
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainLetters()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(65); // A
            Hotkey.AvailableKeyCodes.Should().Contain(90); // Z
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainNumbers()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(48); // 0
            Hotkey.AvailableKeyCodes.Should().Contain(57); // 9
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainNavigationKeys()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(37); // Left
            Hotkey.AvailableKeyCodes.Should().Contain(38); // Up
            Hotkey.AvailableKeyCodes.Should().Contain(39); // Right
            Hotkey.AvailableKeyCodes.Should().Contain(40); // Down
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainMediaKeys()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(179); // MediaPlayPause
            Hotkey.AvailableKeyCodes.Should().Contain(176); // MediaNextTrack
            Hotkey.AvailableKeyCodes.Should().Contain(177); // MediaPreviousTrack
        }

        [Fact]
        public void AvailableKeyCodes_ShouldContainNumpadKeys()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().Contain(96); // Numpad 0
            Hotkey.AvailableKeyCodes.Should().Contain(105); // Numpad 9
        }

        [Fact]
        public void AvailableKeyCodes_ShouldNotBeEmpty()
        {
            // Assert
            Hotkey.AvailableKeyCodes.Should().NotBeEmpty();
            Hotkey.AvailableKeyCodes.Length.Should().BeGreaterThan(50);
        }

        [Fact]
        public void ModifierKeys_ShouldSupportFlagsOperations()
        {
            // Arrange
            var modifiers = ModifierKeys.Control | ModifierKeys.Shift;

            // Assert
            modifiers.HasFlag(ModifierKeys.Control).Should().BeTrue();
            modifiers.HasFlag(ModifierKeys.Shift).Should().BeTrue();
            modifiers.HasFlag(ModifierKeys.Alt).Should().BeFalse();
            modifiers.HasFlag(ModifierKeys.Windows).Should().BeFalse();
        }

        [Fact]
        public void ModifierKeys_ShouldAllowAllModifiersCombined()
        {
            // Arrange
            var allModifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Windows;

            // Assert
            allModifiers.HasFlag(ModifierKeys.Control).Should().BeTrue();
            allModifiers.HasFlag(ModifierKeys.Alt).Should().BeTrue();
            allModifiers.HasFlag(ModifierKeys.Shift).Should().BeTrue();
            allModifiers.HasFlag(ModifierKeys.Windows).Should().BeTrue();
        }

        [Fact]
        public void Hotkey_ShouldAllowDisabling()
        {
            // Arrange
            var hotkey = new Hotkey { IsEnabled = true };

            // Act
            hotkey.IsEnabled = false;

            // Assert
            hotkey.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Hotkey_ShouldSupportMultipleModifiers()
        {
            // Arrange & Act
            var hotkey = new Hotkey
            {
                Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift
            };

            // Assert
            hotkey.Modifiers.HasFlag(ModifierKeys.Control).Should().BeTrue();
            hotkey.Modifiers.HasFlag(ModifierKeys.Alt).Should().BeTrue();
            hotkey.Modifiers.HasFlag(ModifierKeys.Shift).Should().BeTrue();
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
            config.StartWithWindows.Should().BeFalse();
            config.StartMinimized.Should().BeTrue();
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
            config.VolumeSteps = 10;

            // Assert
            config.ClientId.Should().Be("test-id");
            config.ClientSecret.Should().Be("test-secret");
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
