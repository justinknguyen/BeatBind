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
        private readonly Form _parentForm;
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

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<Hotkey>? HotkeyPressed;

        public HotkeyService(Form parentForm, ILogger<HotkeyService> logger)
        {
            _parentForm = parentForm;
            _logger = logger;
            _registeredHotkeys = new Dictionary<int, (Hotkey, Action)>();
            _hookCallback = HookCallback;

            // Set up low-level keyboard hook
            _hookId = SetHook(_hookCallback);
            _logger.LogInformation("Keyboard hook installed");
        }

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

        public void UnregisterAllHotkeys()
        {
            var hotkeyIds = _registeredHotkeys.Keys.ToList();
            foreach (var hotkeyId in hotkeyIds)
            {
                UnregisterHotkey(hotkeyId);
            }
        }

        public bool IsHotkeyRegistered(int hotkeyId)
        {
            return _registeredHotkeys.ContainsKey(hotkeyId);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    _pressedKeys.Add(vkCode);
                    CheckHotkeys();
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _pressedKeys.Remove(vkCode);
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _pressedKeys.Remove(vkCode);
                    ClearInactiveHotkeys();
                }
            }

            // IMPORTANT: Always call next hook to NOT block the key event
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void CheckHotkeys()
        {
            var currentModifiers = GetCurrentModifiers();

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
            }
        }

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

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAllHotkeys();

                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                    _logger.LogInformation("Keyboard hook uninstalled");
                }

                _disposed = true;
            }
        }
    }
}
