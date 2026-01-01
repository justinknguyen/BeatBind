using System.Runtime.InteropServices;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogger<HotkeyService> _logger;
        private readonly Dictionary<int, (Hotkey Hotkey, Action Action)> _registeredHotkeys;
        private bool _disposed;
        private IntPtr _hookId = IntPtr.Zero;
        private readonly HashSet<int> _pressedKeys = new();
        private readonly HashSet<int> _activeHotkeys = new();
        private readonly object _activeLock = new();
        private readonly LowLevelKeyboardProc _hookCallback;

        // Windows API constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        // Windows API functions
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        protected delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<Hotkey>? HotkeyPressed;

        /// <summary>
        /// Initializes a new instance of the HotkeyService class and installs a low-level keyboard hook.
        /// </summary>
        /// <param name="parentForm">The parent form for hotkey registration.</param>
        /// <param name="logger">The logger instance.</param>
        public HotkeyService(ILogger<HotkeyService> logger)
        {
            _logger = logger;
            _registeredHotkeys = new Dictionary<int, (Hotkey, Action)>();
            _hookCallback = HookCallback;

            // Set up low-level keyboard hook
            _hookId = InstallHook(_hookCallback);
            _logger.LogInformation("Keyboard hook installed");
        }

        /// <summary>
        /// Registers a hotkey with an associated action to execute when pressed.
        /// </summary>
        /// <param name="hotkey">The hotkey to register.</param>
        /// <param name="action">The action to execute when the hotkey is pressed.</param>
        /// <returns>True if the hotkey was successfully registered; otherwise, false.</returns>
        public bool RegisterHotkey(Hotkey hotkey, Action action)
        {
            try
            {
                if (_registeredHotkeys.ContainsKey(hotkey.Id))
                {
                    _logger.LogWarning("Hotkey with ID {HotkeyId} is already registered", hotkey.Id);
                    return false;
                }

                _registeredHotkeys[hotkey.Id] = (hotkey, action);
                _logger.LogInformation("Registered hotkey: {Action} (ID: {HotkeyId}, Key: {Key}, Modifiers: {Modifiers})",
                    hotkey.Action, hotkey.Id, (Keys)hotkey.KeyCode, hotkey.Modifiers);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while registering hotkey {HotkeyId}", hotkey.Id);
                return false;
            }
        }

        /// <summary>
        /// Unregisters a hotkey by its ID.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey to unregister.</param>
        /// <returns>True if the hotkey was successfully unregistered; otherwise, false.</returns>
        public bool UnregisterHotkey(int hotkeyId)
        {
            try
            {
                if (!_registeredHotkeys.ContainsKey(hotkeyId))
                {
                    _logger.LogWarning("Hotkey with ID {HotkeyId} is not registered", hotkeyId);
                    return false;
                }

                var hotkey = _registeredHotkeys[hotkeyId].Hotkey;
                _registeredHotkeys.Remove(hotkeyId);
                _logger.LogInformation("Unregistered hotkey: {Action} (ID: {HotkeyId})", hotkey.Action, hotkeyId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while unregistering hotkey {HotkeyId}", hotkeyId);
                return false;
            }
        }

        /// <summary>
        /// Unregisters all currently registered hotkeys.
        /// </summary>
        public void UnregisterAllHotkeys()
        {
            var hotkeyIds = _registeredHotkeys.Keys.ToList();
            foreach (var hotkeyId in hotkeyIds)
            {
                UnregisterHotkey(hotkeyId);
            }
        }

        /// <summary>
        /// Checks if a hotkey with the specified ID is currently registered.
        /// </summary>
        /// <param name="hotkeyId">The hotkey ID to check.</param>
        /// <returns>True if the hotkey is registered; otherwise, false.</returns>
        public bool IsHotkeyRegistered(int hotkeyId)
        {
            return _registeredHotkeys.ContainsKey(hotkeyId);
        }

        /// <summary>
        /// Pauses the hotkey service by removing the keyboard hook.
        /// </summary>
        public void Pause()
        {
            if (_hookId != IntPtr.Zero)
            {
                UninstallHook(_hookId);
                _hookId = IntPtr.Zero;
                _logger.LogInformation("Keyboard hook paused");
            }
        }

        /// <summary>
        /// Resumes the hotkey service by reinstalling the keyboard hook.
        /// </summary>
        public void Resume()
        {
            if (_hookId == IntPtr.Zero)
            {
                _hookId = InstallHook(_hookCallback);
                _logger.LogInformation("Keyboard hook resumed");
            }
        }

        /// <summary>
        /// Installs a low-level keyboard hook to intercept keyboard events.
        /// </summary>
        /// <param name="proc">The callback procedure for the hook.</param>
        /// <returns>A handle to the installed hook.</returns>
        protected virtual IntPtr InstallHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
        }

        /// <summary>
        /// Uninstalls the low-level keyboard hook.
        /// </summary>
        /// <param name="hookId">The handle to the hook to uninstall.</param>
        protected virtual void UninstallHook(IntPtr hookId)
        {
            UnhookWindowsHookEx(hookId);
        }

        /// <summary>
        /// Processes keyboard events from the low-level keyboard hook.
        /// Tracks pressed and released keys and triggers hotkey checks.
        /// </summary>
        /// <param name="nCode">The hook code.</param>
        /// <param name="wParam">The message identifier.</param>
        /// <param name="lParam">A pointer to keyboard event information.</param>
        /// <returns>The result of the next hook in the chain, or 1 to suppress the key event.</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    _pressedKeys.Add(vkCode);

                    // Check if this key press triggers a hotkey
                    bool hotkeyTriggered = CheckHotkeys();

                    // If a hotkey was triggered, suppress the key event from propagating
                    if (hotkeyTriggered)
                    {
                        return (IntPtr)1;
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _pressedKeys.Remove(vkCode);
                    ClearInactiveHotkeys();
                }
            }

            // Allow the key event to propagate if no hotkey was triggered
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Checks all registered hotkeys against currently pressed keys and executes matching hotkey actions.
        /// </summary>
        /// <returns>True if a hotkey was triggered; otherwise, false.</returns>
        private bool CheckHotkeys()
        {
            var currentModifiers = GetCurrentModifiers();
            bool hotkeyTriggered = false;

            foreach (var (hotkeyId, hotkeyInfo) in _registeredHotkeys)
            {
                if (!hotkeyInfo.Hotkey.IsEnabled)
                {
                    continue;
                }

                // Check if the main key is pressed
                if (!_pressedKeys.Contains(hotkeyInfo.Hotkey.KeyCode))
                {
                    continue;
                }

                // Check if modifiers match
                if (hotkeyInfo.Hotkey.Modifiers != currentModifiers)
                {
                    continue;
                }

                // Prevent flooding while the key is held down
                lock (_activeLock)
                {
                    if (_activeHotkeys.Contains(hotkeyId))
                    {
                        continue;
                    }

                    _activeHotkeys.Add(hotkeyId);
                }

                // Hotkey matched! Execute action off the UI thread for responsiveness
                _ = Task.Run(() => ExecuteHotkeyAsync(hotkeyId, hotkeyInfo));
                hotkeyTriggered = true;
            }

            return hotkeyTriggered;
        }

        /// <summary>
        /// Executes a hotkey action asynchronously and raises the HotkeyPressed event.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey being executed.</param>
        /// <param name="hotkeyInfo">The hotkey and action information.</param>
        /// <returns>A completed task.</returns>
        private Task ExecuteHotkeyAsync(int hotkeyId, (Hotkey Hotkey, Action Action) hotkeyInfo)
        {
            try
            {
                _logger.LogDebug("Hotkey triggered: {Action} (ID: {HotkeyId})", hotkeyInfo.Hotkey.Action, hotkeyId);
                hotkeyInfo.Action?.Invoke();
                HotkeyPressed?.Invoke(this, hotkeyInfo.Hotkey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing hotkey action for {HotkeyId}", hotkeyId);
            }
            finally
            {
                lock (_activeLock)
                {
                    _activeHotkeys.Remove(hotkeyId);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines which modifier keys (Ctrl, Alt, Shift, Windows) are currently pressed.
        /// </summary>
        /// <returns>A ModifierKeys enum value representing the currently pressed modifiers.</returns>
        private ModifierKeys GetCurrentModifiers()
        {
            var modifiers = ModifierKeys.None;

            if (_pressedKeys.Contains((int)Keys.LControlKey) || _pressedKeys.Contains((int)Keys.RControlKey) || _pressedKeys.Contains((int)Keys.ControlKey))
            {
                modifiers |= ModifierKeys.Control;
            }

            if (_pressedKeys.Contains((int)Keys.LMenu) || _pressedKeys.Contains((int)Keys.RMenu) || _pressedKeys.Contains((int)Keys.Menu))
            {
                modifiers |= ModifierKeys.Alt;
            }

            if (_pressedKeys.Contains((int)Keys.LShiftKey) || _pressedKeys.Contains((int)Keys.RShiftKey) || _pressedKeys.Contains((int)Keys.ShiftKey))
            {
                modifiers |= ModifierKeys.Shift;
            }

            if (_pressedKeys.Contains((int)Keys.LWin) || _pressedKeys.Contains((int)Keys.RWin))
            {
                modifiers |= ModifierKeys.Windows;
            }

            return modifiers;
        }

        /// <summary>
        /// Removes hotkeys from the active set when their keys are no longer pressed.
        /// </summary>
        private void ClearInactiveHotkeys()
        {
            lock (_activeLock)
            {
                if (_activeHotkeys.Count == 0)
                {
                    return;
                }

                var currentModifiers = GetCurrentModifiers();
                var toRemove = new List<int>();

                foreach (var hotkeyId in _activeHotkeys)
                {
                    if (!_registeredHotkeys.TryGetValue(hotkeyId, out var info))
                    {
                        toRemove.Add(hotkeyId);
                        continue;
                    }

                    var mainKeyStillDown = _pressedKeys.Contains(info.Hotkey.KeyCode);
                    var modifiersMatch = info.Hotkey.Modifiers == currentModifiers;

                    if (!mainKeyStillDown || !modifiersMatch)
                    {
                        toRemove.Add(hotkeyId);
                    }
                }

                foreach (var id in toRemove)
                {
                    _activeHotkeys.Remove(id);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the HotkeyService, including unregistering hotkeys and removing the keyboard hook.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAllHotkeys();

                if (_hookId != IntPtr.Zero)
                {
                    UninstallHook(_hookId);
                    _hookId = IntPtr.Zero;
                    _logger.LogInformation("Keyboard hook uninstalled");
                }

                _disposed = true;
            }
        }
    }
}
