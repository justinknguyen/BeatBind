using System.Runtime.InteropServices;
using System.Windows.Forms;
using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Hotkeys
{
    public class WindowsHotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogger<WindowsHotkeyService> _logger;
        private readonly Dictionary<int, (Hotkey Hotkey, Action Action)> _registeredHotkeys;
        private readonly Form _parentForm;
        private bool _disposed;

        // Windows API constants
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        // Windows API functions
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public event EventHandler<Hotkey>? HotkeyPressed;

        public WindowsHotkeyService(Form parentForm, ILogger<WindowsHotkeyService> logger)
        {
            _parentForm = parentForm;
            _logger = logger;
            _registeredHotkeys = new Dictionary<int, (Hotkey, Action)>();

            // Hook into the form's message processing
            if (_parentForm.IsHandleCreated)
            {
                SetupMessageFilter();
            }
            else
            {
                _parentForm.HandleCreated += OnFormHandleCreated;
            }
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

                var modifiers = ConvertModifiers(hotkey.Modifiers);
                var virtualKey = (uint)hotkey.KeyCode; // Use KeyCode instead of Key

                var success = RegisterHotKey(_parentForm.Handle, hotkey.Id, modifiers, virtualKey);
                
                if (success)
                {
                    _registeredHotkeys[hotkey.Id] = (hotkey, action);
                    _logger.LogInformation("Registered hotkey: {Action} (ID: {HotkeyId})", hotkey.Action, hotkey.Id);
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to register hotkey {HotkeyId}. Win32 Error: {Error}", hotkey.Id, error);
                }

                return success;
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

                var success = UnregisterHotKey(_parentForm.Handle, hotkeyId);
                
                if (success)
                {
                    var hotkey = _registeredHotkeys[hotkeyId].Hotkey;
                    _registeredHotkeys.Remove(hotkeyId);
                    _logger.LogInformation("Unregistered hotkey: {Action} (ID: {HotkeyId})", hotkey.Action, hotkeyId);
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to unregister hotkey {HotkeyId}. Win32 Error: {Error}", hotkeyId, error);
                }

                return success;
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

        private void OnFormHandleCreated(object? sender, EventArgs e)
        {
            SetupMessageFilter();
        }

        private void SetupMessageFilter()
        {
            // Add a message filter to capture WM_HOTKEY messages
            Application.AddMessageFilter(new HotkeyMessageFilter(this));
        }

        private void OnHotkeyMessage(int hotkeyId)
        {
            if (_registeredHotkeys.TryGetValue(hotkeyId, out var hotkeyInfo))
            {
                try
                {
                    _logger.LogDebug("Hotkey triggered: {Action} (ID: {HotkeyId})", hotkeyInfo.Hotkey.Action, hotkeyId);
                    
                    // Invoke the action
                    hotkeyInfo.Action?.Invoke();
                    
                    // Fire the event
                    HotkeyPressed?.Invoke(this, hotkeyInfo.Hotkey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing hotkey action for {HotkeyId}", hotkeyId);
                }
            }
        }

        private static uint ConvertModifiers(ModifierKeys modifiers)
        {
            uint result = 0;
            
            if (modifiers.HasFlag(ModifierKeys.Alt))
                result |= MOD_ALT;
            if (modifiers.HasFlag(ModifierKeys.Control))
                result |= MOD_CONTROL;
            if (modifiers.HasFlag(ModifierKeys.Shift))
                result |= MOD_SHIFT;
            if (modifiers.HasFlag(ModifierKeys.Windows))
                result |= MOD_WIN;

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAllHotkeys();
                _disposed = true;
            }
        }

        private class HotkeyMessageFilter : IMessageFilter
        {
            private readonly WindowsHotkeyService _hotkeyService;

            public HotkeyMessageFilter(WindowsHotkeyService hotkeyService)
            {
                _hotkeyService = hotkeyService;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    var hotkeyId = m.WParam.ToInt32();
                    _hotkeyService.OnHotkeyMessage(hotkeyId);
                    return true; // Message handled
                }
                return false; // Message not handled
            }
        }
    }
}
