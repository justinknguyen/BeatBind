using BeatBind.Core.Entities;
using BeatBind.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeatBind.Tests.Infrastructure.Services
{
    public class HotkeyServiceTests
    {
        private readonly Mock<ILogger<HotkeyService>> _mockLogger;
        private readonly Mock<Form> _mockForm;
        private readonly TestableHotkeyService _service;

        public HotkeyServiceTests()
        {
            _mockLogger = new Mock<ILogger<HotkeyService>>();
            _mockForm = new Mock<Form>();
            _service = new TestableHotkeyService(_mockForm.Object, _mockLogger.Object);
        }

        [Fact]
        public void RegisterHotkey_ShouldAddHotkey()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, KeyCode = 65 };
            var action = () => { };

            // Act
            var result = _service.RegisterHotkey(hotkey, action);

            // Assert
            result.Should().BeTrue();
            _service.IsHotkeyRegistered(1).Should().BeTrue();
        }

        [Fact]
        public void RegisterHotkey_WhenAlreadyRegistered_ShouldReturnFalse()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, KeyCode = 65 };
            var action = () => { };
            _service.RegisterHotkey(hotkey, action);

            // Act
            var result = _service.RegisterHotkey(hotkey, action);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void UnregisterHotkey_ShouldRemoveHotkey()
        {
            // Arrange
            var hotkey = new Hotkey { Id = 1, Action = HotkeyAction.PlayPause, KeyCode = 65 };
            var action = () => { };
            _service.RegisterHotkey(hotkey, action);

            // Act
            var result = _service.UnregisterHotkey(1);

            // Assert
            result.Should().BeTrue();
            _service.IsHotkeyRegistered(1).Should().BeFalse();
        }

        [Fact]
        public void UnregisterHotkey_WhenNotRegistered_ShouldReturnFalse()
        {
            // Act
            var result = _service.UnregisterHotkey(99);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void UnregisterAllHotkeys_ShouldRemoveAll()
        {
            // Arrange
            _service.RegisterHotkey(new Hotkey { Id = 1 }, () => { });
            _service.RegisterHotkey(new Hotkey { Id = 2 }, () => { });

            // Act
            _service.UnregisterAllHotkeys();

            // Assert
            _service.IsHotkeyRegistered(1).Should().BeFalse();
            _service.IsHotkeyRegistered(2).Should().BeFalse();
        }

        [Fact]
        public void Pause_ShouldUninstallHook()
        {
            // Act
            _service.Pause();

            // Assert
            _service.IsHookInstalled.Should().BeFalse();
        }

        [Fact]
        public void Resume_ShouldInstallHook()
        {
            // Arrange
            _service.Pause();

            // Act
            _service.Resume();

            // Assert
            _service.IsHookInstalled.Should().BeTrue();
        }

        // Testable subclass to bypass P/Invoke
        private class TestableHotkeyService : HotkeyService
        {
            public bool IsHookInstalled { get; private set; }

            public TestableHotkeyService(Form parentForm, ILogger<HotkeyService> logger)
                : base(parentForm, logger)
            {
            }

            protected override IntPtr InstallHook(LowLevelKeyboardProc proc)
            {
                IsHookInstalled = true;
                return new IntPtr(123); // Dummy handle
            }

            protected override void UninstallHook(IntPtr hookId)
            {
                IsHookInstalled = false;
            }
        }
    }
}
