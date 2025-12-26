using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class HotkeyApplicationServiceTests
    {
        private readonly Mock<IHotkeyService> _mockHotkeyService;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<MusicControlApplicationService> _mockMusicControlService;
        private readonly Mock<ILogger<HotkeyApplicationService>> _mockLogger;
        private readonly HotkeyApplicationService _service;

        public HotkeyApplicationServiceTests()
        {
            _mockHotkeyService = new Mock<IHotkeyService>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockMusicControlService = new Mock<MusicControlApplicationService>(
                Mock.Of<ISpotifyService>(),
                Mock.Of<IConfigurationService>(),
                Mock.Of<ILogger<MusicControlApplicationService>>());
            _mockLogger = new Mock<ILogger<HotkeyApplicationService>>();
            
            _service = new HotkeyApplicationService(
                _mockHotkeyService.Object,
                _mockConfigService.Object,
                _mockMusicControlService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void InitializeHotkeys_ShouldRegisterAllEnabledHotkeys()
        {
            // Arrange
            var hotkeys = new List<Hotkey>
            {
                new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 },
                new Hotkey { Id = 2, Action = HotkeyAction.NextTrack, IsEnabled = true, KeyCode = 0xB0 },
                new Hotkey { Id = 3, Action = HotkeyAction.PreviousTrack, IsEnabled = false, KeyCode = 0xB1 }
            };
            _mockConfigService.Setup(x => x.GetHotkeys()).Returns(hotkeys);
            _mockHotkeyService.Setup(x => x.RegisterHotkey(It.IsAny<Hotkey>(), It.IsAny<Action>())).Returns(true);

            // Act
            _service.InitializeHotkeys();

            // Assert
            _mockHotkeyService.Verify(x => x.RegisterHotkey(It.IsAny<Hotkey>(), It.IsAny<Action>()), Times.Exactly(2));
        }

        [Fact]
        public void RegisterHotkey_ShouldCallHotkeyService()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            _mockHotkeyService.Setup(x => x.RegisterHotkey(hotkey, It.IsAny<Action>())).Returns(true);

            // Act
            var result = _service.RegisterHotkey(hotkey);

            // Assert
            result.Should().BeTrue();
            _mockHotkeyService.Verify(x => x.RegisterHotkey(hotkey, It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public void UnregisterHotkey_ShouldCallHotkeyService()
        {
            // Arrange
            _mockHotkeyService.Setup(x => x.UnregisterHotkey(1)).Returns(true);

            // Act
            var result = _service.UnregisterHotkey(1);

            // Assert
            result.Should().BeTrue();
            _mockHotkeyService.Verify(x => x.UnregisterHotkey(1), Times.Once);
        }

        [Fact]
        public void AddHotkey_WhenEnabled_ShouldRegisterAndSave()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            _mockHotkeyService.Setup(x => x.RegisterHotkey(hotkey, It.IsAny<Action>())).Returns(true);

            // Act
            _service.AddHotkey(hotkey);

            // Assert
            _mockConfigService.Verify(x => x.AddHotkey(hotkey), Times.Once);
            _mockHotkeyService.Verify(x => x.RegisterHotkey(hotkey, It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public void AddHotkey_WhenDisabled_ShouldOnlySave()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = false, KeyCode = 0xB3 };

            // Act
            _service.AddHotkey(hotkey);

            // Assert
            _mockConfigService.Verify(x => x.AddHotkey(hotkey), Times.Once);
            _mockHotkeyService.Verify(x => x.RegisterHotkey(It.IsAny<Hotkey>(), It.IsAny<Action>()), Times.Never);
        }

        [Fact]
        public void RemoveHotkey_ShouldUnregisterAndDelete()
        {
            // Arrange
            _mockHotkeyService.Setup(x => x.UnregisterHotkey(1)).Returns(true);

            // Act
            _service.RemoveHotkey(1);

            // Assert
            _mockHotkeyService.Verify(x => x.UnregisterHotkey(1), Times.Once);
            _mockConfigService.Verify(x => x.RemoveHotkey(1), Times.Once);
        }

        [Fact]
        public void UpdateHotkey_WhenPreviouslyRegistered_ShouldUnregisterThenRegister()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.NextTrack, IsEnabled = true, KeyCode = 0xB0 };
            _mockHotkeyService.Setup(x => x.IsHotkeyRegistered(1)).Returns(true);
            _mockHotkeyService.Setup(x => x.UnregisterHotkey(1)).Returns(true);
            _mockHotkeyService.Setup(x => x.RegisterHotkey(hotkey, It.IsAny<Action>())).Returns(true);

            // Act
            _service.UpdateHotkey(hotkey);

            // Assert
            _mockHotkeyService.Verify(x => x.UnregisterHotkey(1), Times.Once);
            _mockConfigService.Verify(x => x.UpdateHotkey(hotkey), Times.Once);
            _mockHotkeyService.Verify(x => x.RegisterHotkey(hotkey, It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public void UpdateHotkey_WhenDisabled_ShouldNotRegister()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.NextTrack, IsEnabled = false, KeyCode = 0xB0 };
            _mockHotkeyService.Setup(x => x.IsHotkeyRegistered(1)).Returns(true);
            _mockHotkeyService.Setup(x => x.UnregisterHotkey(1)).Returns(true);

            // Act
            _service.UpdateHotkey(hotkey);

            // Assert
            _mockHotkeyService.Verify(x => x.UnregisterHotkey(1), Times.Once);
            _mockConfigService.Verify(x => x.UpdateHotkey(hotkey), Times.Once);
            _mockHotkeyService.Verify(x => x.RegisterHotkey(It.IsAny<Hotkey>(), It.IsAny<Action>()), Times.Never);
        }

        [Fact]
        public void HotkeyTriggered_ShouldRaiseEvent()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            Hotkey? triggeredHotkey = null;
            _service.HotkeyTriggered += (sender, h) => triggeredHotkey = h;

            // Act
            _mockHotkeyService.Raise(x => x.HotkeyPressed += null, _mockHotkeyService.Object, hotkey);

            // Assert
            triggeredHotkey.Should().Be(hotkey);
        }

        [Fact]
        public void RegisterHotkey_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, IsEnabled = true, KeyCode = 0xB3 };
            _mockHotkeyService.Setup(x => x.RegisterHotkey(hotkey, It.IsAny<Action>())).Throws(new Exception("Registration failed"));

            // Act
            var result = _service.RegisterHotkey(hotkey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void UnregisterHotkey_WhenExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            _mockHotkeyService.Setup(x => x.UnregisterHotkey(1)).Throws(new Exception("Unregistration failed"));

            // Act
            var result = _service.UnregisterHotkey(1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void UpdateHotkey_WhenNotPreviouslyRegistered_ShouldOnlyUpdateAndRegister()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.NextTrack, IsEnabled = true, KeyCode = 0xB0 };
            _mockHotkeyService.Setup(x => x.IsHotkeyRegistered(1)).Returns(false);
            _mockHotkeyService.Setup(x => x.RegisterHotkey(hotkey, It.IsAny<Action>())).Returns(true);

            // Act
            _service.UpdateHotkey(hotkey);

            // Assert
            _mockHotkeyService.Verify(x => x.UnregisterHotkey(1), Times.Never);
            _mockConfigService.Verify(x => x.UpdateHotkey(hotkey), Times.Once);
            _mockHotkeyService.Verify(x => x.RegisterHotkey(hotkey, It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldUnregisterAllHotkeys()
        {
            // Act
            _service.Dispose();

            // Assert
            _mockHotkeyService.Verify(x => x.UnregisterAllHotkeys(), Times.Once);
        }    }
}