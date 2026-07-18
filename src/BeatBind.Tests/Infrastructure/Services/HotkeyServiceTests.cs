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

        // Virtual key codes used by the key-simulation tests
        private const int VkLWin = 0x5B;
        private const int VkLMenu = 0xA4; // left Alt
        private const int VkNumPad3 = 0x63;

        // Regression test for the Win+L lock bug: the Win key-down is seen by the
        // hook, but its key-up happens on the secure desktop where low-level hooks
        // receive no events. Without pruning, the phantom Win modifier blocks all
        // hotkey matching until the app is restarted.
        [Fact]
        public void Hotkey_AfterKeyUpLostToSecureDesktop_ShouldStillTrigger()
        {
            var service = new KeySimulatingHotkeyService(_mockLogger.Object);
            var triggered = new ManualResetEventSlim(false);
            service.RegisterHotkey(
                new Hotkey { Id = 1, Action = HotkeyAction.VolumeDown, KeyCode = VkNumPad3, Modifiers = ModifierKeys.Alt },
                () => triggered.Set());

            // Win+L: key-down observed, key-up swallowed by the lock screen
            service.SimulateKeyDown(VkLWin);
            service.SimulateLostKeyUp(VkLWin);

            // After unlock, the user presses Alt+NumPad3
            service.SimulateKeyDown(VkLMenu);
            var suppressed = service.SimulateKeyDown(VkNumPad3);

            suppressed.Should().BeTrue("the phantom Win key should be pruned so the hotkey matches");
            triggered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        }

        [Fact]
        public void Hotkey_WithHeldModifier_ShouldNotBePrunedAndShouldTrigger()
        {
            var service = new KeySimulatingHotkeyService(_mockLogger.Object);
            var triggered = new ManualResetEventSlim(false);
            service.RegisterHotkey(
                new Hotkey { Id = 1, Action = HotkeyAction.VolumeDown, KeyCode = VkNumPad3, Modifiers = ModifierKeys.Alt },
                () => triggered.Set());

            // Normal use: Alt is genuinely held when the main key goes down
            service.SimulateKeyDown(VkLMenu);
            var suppressed = service.SimulateKeyDown(VkNumPad3);

            suppressed.Should().BeTrue();
            triggered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        }

        // A key-down this hook suppresses never reaches the OS input state, so
        // GetAsyncKeyState reports it as up while it is physically held. The key
        // currently being processed must therefore be excluded from pruning.
        [Fact]
        public void Hotkey_WhoseKeyDownIsNotReflectedInOsState_ShouldStillTrigger()
        {
            var service = new KeySimulatingHotkeyService(_mockLogger.Object);
            var triggered = new ManualResetEventSlim(false);
            service.RegisterHotkey(
                new Hotkey { Id = 1, Action = HotkeyAction.VolumeDown, KeyCode = VkNumPad3, Modifiers = ModifierKeys.Alt },
                () => triggered.Set());

            service.SimulateKeyDown(VkLMenu);
            var suppressed = service.SimulateKeyDownInvisibleToOS(VkNumPad3);

            suppressed.Should().BeTrue("the current key must not be pruned even if the OS state does not show it down");
            triggered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        }

        [Fact]
        public void Hotkey_AfterMainKeyUpLostToSecureDesktop_ShouldTriggerOnNextPress()
        {
            var service = new KeySimulatingHotkeyService(_mockLogger.Object);
            var triggerCount = 0;
            var triggered = new SemaphoreSlim(0);
            service.RegisterHotkey(
                new Hotkey { Id = 1, Action = HotkeyAction.VolumeDown, KeyCode = VkNumPad3, Modifiers = ModifierKeys.Alt },
                () => { Interlocked.Increment(ref triggerCount); triggered.Release(); });

            // Hotkey fires, then both key-ups are lost (e.g. locked mid-press)
            service.SimulateKeyDown(VkLMenu);
            service.SimulateKeyDown(VkNumPad3);
            triggered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            service.SimulateLostKeyUp(VkNumPad3);
            service.SimulateLostKeyUp(VkLMenu);

            // After unlock the same hotkey is pressed again
            service.SimulateKeyDown(VkLMenu);
            service.SimulateKeyDown(VkNumPad3);

            triggered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            triggerCount.Should().Be(2);
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

        // Simulates key events and OS key state without P/Invoke. "Lost" key-ups
        // update the simulated OS state but never reach the hook — exactly what
        // happens when Windows switches to the secure desktop (Win+L, UAC).
        private class KeySimulatingHotkeyService : HotkeyService
        {
            private readonly HashSet<int> _osKeysDown = new();

            public KeySimulatingHotkeyService(ILogger<HotkeyService> logger) : base(logger) { }

            public bool SimulateKeyDown(int vkCode)
            {
                _osKeysDown.Add(vkCode);
                return ProcessKeyDown(vkCode);
            }

            // A key-down whose event was suppressed never registers in the OS input state
            public bool SimulateKeyDownInvisibleToOS(int vkCode)
            {
                return ProcessKeyDown(vkCode);
            }

            // The key is physically released but the hook never sees the key-up event
            public void SimulateLostKeyUp(int vkCode)
            {
                _osKeysDown.Remove(vkCode);
            }

            protected override bool IsKeyDownInOS(int vkCode) => _osKeysDown.Contains(vkCode);

            protected override IntPtr InstallHook(LowLevelKeyboardProc proc) => new IntPtr(123);

            protected override void UninstallHook(IntPtr hookId) { }
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
