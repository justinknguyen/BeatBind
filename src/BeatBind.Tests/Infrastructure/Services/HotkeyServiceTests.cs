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
        private readonly TestableHotkeyService _service;

        public HotkeyServiceTests()
        {
            _mockLogger = new Mock<ILogger<HotkeyService>>();
            _service = new TestableHotkeyService(_mockLogger.Object);
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

        // Verifies the Pause+Resume cycle used by the startup timing fix in MainForm:
        // the hook is reinstalled on the first Application.Idle tick to replace any
        // hook that Windows may have timed out before the message loop started.
        [Fact]
        public void PauseThenResume_ShouldReinstallHook()
        {
            // Constructor installs hook once (InstallCallCount = 1)
            _service.Pause();
            _service.Resume();

            _service.IsHookInstalled.Should().BeTrue();
            _service.InstallCallCount.Should().Be(2); // ctor + Resume
            _service.UninstallCallCount.Should().Be(1);
        }

        [Fact]
        public void Resume_WhenHookAlreadyInstalled_ShouldNotInstallAgain()
        {
            // Hook is installed from constructor — calling Resume again should be a no-op
            _service.Resume();

            _service.IsHookInstalled.Should().BeTrue();
            _service.InstallCallCount.Should().Be(1); // only from ctor
        }

        [Fact]
        public void Pause_WhenHookAlreadyPaused_ShouldNotUninstallAgain()
        {
            _service.Pause();
            _service.Pause(); // second call should be a no-op

            _service.UninstallCallCount.Should().Be(1);
        }

        [Fact]
        public void Constructor_WhenHookInstallFails_ShouldLogError()
        {
            // A zero return from SetWindowsHookEx means installation failed
            var failingService = new FailingHookTestableService(_mockLogger.Object);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("failed to install")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Resume_WhenHookInstallFails_ShouldLogError()
        {
            _service.Pause();
            _service.ForceFailNextInstall = true;
            _service.Resume();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("failed to resume")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // Testable subclass to bypass P/Invoke
        private class TestableHotkeyService : HotkeyService
        {
            public bool IsHookInstalled { get; private set; }
            public int InstallCallCount { get; private set; }
            public int UninstallCallCount { get; private set; }
            public bool ForceFailNextInstall { get; set; }

            public TestableHotkeyService(ILogger<HotkeyService> logger)
                : base(logger)
            {
            }

            protected override IntPtr InstallHook(LowLevelKeyboardProc proc)
            {
                if (ForceFailNextInstall)
                {
                    ForceFailNextInstall = false;
                    return IntPtr.Zero;
                }
                IsHookInstalled = true;
                InstallCallCount++;
                return new IntPtr(123); // Dummy handle
            }

            protected override void UninstallHook(IntPtr hookId)
            {
                IsHookInstalled = false;
                UninstallCallCount++;
            }
        }

        // Always fails to install the hook — used to test error logging in the constructor
        private class FailingHookTestableService : HotkeyService
        {
            public FailingHookTestableService(ILogger<HotkeyService> logger) : base(logger) { }

            protected override IntPtr InstallHook(LowLevelKeyboardProc proc) => IntPtr.Zero;

            protected override void UninstallHook(IntPtr hookId) { }
        }
    }
}
