using BeatBind.Application.Services;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Application.Services
{
    public class HotkeyManagementServiceTests
    {
        private readonly Mock<IHotkeyService> _mockHotkeyService;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<MusicControlService> _mockMusicControlService;
        private readonly Mock<ILogger<HotkeyManagementService>> _mockLogger;
        private readonly HotkeyManagementService _service;

        public HotkeyManagementServiceTests()
        {
            _mockHotkeyService = new Mock<IHotkeyService>();
            _mockConfigService = new Mock<IConfigurationService>();
            _mockMusicControlService = new Mock<MusicControlService>(
                Mock.Of<ISpotifyService>(),
                Mock.Of<IConfigurationService>(),
                Mock.Of<ILogger<MusicControlService>>());
            _mockLogger = new Mock<ILogger<HotkeyManagementService>>();
            
            _service = new HotkeyManagementService(
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
    }
}
