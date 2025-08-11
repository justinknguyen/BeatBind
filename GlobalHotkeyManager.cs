using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    public class GlobalHotkeyManager : IDisposable
    {
        private readonly ILogger<GlobalHotkeyManager> _logger;
        private readonly Dictionary<int, Action> _hotkeyActions;
        private readonly Form _form;
        private int _hotkeyId;
        private bool _disposed;

        // Windows API constants
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        // Windows API functions
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public GlobalHotkeyManager(Form parentForm)
        {
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<GlobalHotkeyManager>();
            _hotkeyActions = new Dictionary<int, Action>();
            _form = parentForm;
            _hotkeyId = 1;

            // Override WndProc to handle hotkey messages
            var originalWndProc = _form.GetType().GetMethod("WndProc", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (originalWndProc != null)
            {
                // This is a simplified approach - in a real implementation, you'd want to properly override WndProc
                _form.HandleCreated += OnFormHandleCreated;
            }
        }

        private void OnFormHandleCreated(object? sender, EventArgs e)
        {
            // Set up message filter for hotkeys
            Application.AddMessageFilter(new HotkeyMessageFilter(this));
        }

        public bool RegisterHotkey(string hotkeyString, Action action)
        {
            try
            {
                if (ParseHotkey(hotkeyString, out uint modifiers, out uint vkey))
                {
                    var id = _hotkeyId++;
                    if (RegisterHotKey(_form.Handle, id, modifiers, vkey))
                    {
                        _hotkeyActions[id] = action;
                        _logger.LogInformation($"Registered hotkey: {hotkeyString} with ID {id}");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to register hotkey: {hotkeyString}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Invalid hotkey format: {hotkeyString}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering hotkey: {hotkeyString}");
            }

            return false;
        }

        public void UnregisterAllHotkeys()
        {
            try
            {
                foreach (var id in _hotkeyActions.Keys)
                {
                    UnregisterHotKey(_form.Handle, id);
                    _logger.LogInformation($"Unregistered hotkey with ID {id}");
                }
                _hotkeyActions.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering hotkeys");
            }
        }

        internal void ProcessHotkeyMessage(int hotkeyId)
        {
            if (_hotkeyActions.TryGetValue(hotkeyId, out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing hotkey action for ID {hotkeyId}");
                }
            }
        }

        private bool ParseHotkey(string hotkeyString, out uint modifiers, out uint vkey)
        {
            modifiers = 0;
            vkey = 0;

            if (string.IsNullOrEmpty(hotkeyString))
                return false;

            var parts = hotkeyString.Split('+');
            if (parts.Length == 0)
                return false;

            // Parse modifiers
            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].Trim().ToLower())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "win":
                    case "windows":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        return false;
                }
            }

            // Parse key
            var keyString = parts[parts.Length - 1].Trim().ToUpper();
            if (Enum.TryParse<Keys>(keyString, out var key))
            {
                vkey = (uint)key;
                return true;
            }

            // Handle special keys
            switch (keyString.ToLower())
            {
                case "space":
                    vkey = (uint)Keys.Space;
                    return true;
                case "left":
                    vkey = (uint)Keys.Left;
                    return true;
                case "right":
                    vkey = (uint)Keys.Right;
                    return true;
                case "up":
                    vkey = (uint)Keys.Up;
                    return true;
                case "down":
                    vkey = (uint)Keys.Down;
                    return true;
                default:
                    return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAllHotkeys();
                _disposed = true;
            }
        }
    }

    internal class HotkeyMessageFilter : IMessageFilter
    {
        private readonly GlobalHotkeyManager _hotkeyManager;
        private const int WM_HOTKEY = 0x0312;

        public HotkeyMessageFilter(GlobalHotkeyManager hotkeyManager)
        {
            _hotkeyManager = hotkeyManager;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                var hotkeyId = m.WParam.ToInt32();
                _hotkeyManager.ProcessHotkeyMessage(hotkeyId);
                return true;
            }
            return false;
        }
    }
}
