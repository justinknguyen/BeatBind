using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class HotkeyApplicationService
    {
        private readonly IHotkeyService _hotkeyService;
        private readonly IConfigurationService _configurationService;
        private readonly MusicControlApplicationService _musicControlService;
        private readonly ILogger<HotkeyApplicationService> _logger;

        public event EventHandler<Hotkey>? HotkeyTriggered;

        public HotkeyApplicationService(
            IHotkeyService hotkeyService,
            IConfigurationService configurationService,
            MusicControlApplicationService musicControlService,
            ILogger<HotkeyApplicationService> logger)
        {
            _hotkeyService = hotkeyService;
            _configurationService = configurationService;
            _musicControlService = musicControlService;
            _logger = logger;

            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        public void InitializeHotkeys()
        {
            var hotkeys = _configurationService.GetHotkeys();
            foreach (var hotkey in hotkeys.Where(h => h.IsEnabled))
            {
                RegisterHotkey(hotkey);
            }
        }

        public bool RegisterHotkey(Hotkey hotkey)
        {
            try
            {
                var action = GetActionForHotkey(hotkey.Action);
                return _hotkeyService.RegisterHotkey(hotkey, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register hotkey {HotkeyId}", hotkey.Id);
                return false;
            }
        }

        public bool UnregisterHotkey(int hotkeyId)
        {
            try
            {
                return _hotkeyService.UnregisterHotkey(hotkeyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister hotkey {HotkeyId}", hotkeyId);
                return false;
            }
        }

        public void AddHotkey(Hotkey hotkey)
        {
            _configurationService.AddHotkey(hotkey);
            if (hotkey.IsEnabled)
            {
                RegisterHotkey(hotkey);
            }
        }

        public void RemoveHotkey(int hotkeyId)
        {
            UnregisterHotkey(hotkeyId);
            _configurationService.RemoveHotkey(hotkeyId);
        }

        public void UpdateHotkey(Hotkey hotkey)
        {
            // Unregister old hotkey if it exists
            if (_hotkeyService.IsHotkeyRegistered(hotkey.Id))
            {
                UnregisterHotkey(hotkey.Id);
            }

            _configurationService.UpdateHotkey(hotkey);

            // Register new hotkey if enabled
            if (hotkey.IsEnabled)
            {
                RegisterHotkey(hotkey);
            }
        }

        private void OnHotkeyPressed(object? sender, Hotkey hotkey)
        {
            try
            {
                _logger.LogInformation("Hotkey pressed: {Action}", hotkey.Action);

                // Notify subscribers that a hotkey was triggered
                HotkeyTriggered?.Invoke(this, hotkey);

                // Action is already executed by the hotkey service via the registered callback
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle hotkey press for {Action}", hotkey.Action);
            }
        }

        private Action GetActionForHotkey(HotkeyAction hotkeyAction)
        {
            return async () => await ExecuteHotkeyAction(hotkeyAction);
        }

        private async Task ExecuteHotkeyAction(HotkeyAction action)
        {
            switch (action)
            {
                case HotkeyAction.PlayPause:
                    await _musicControlService.PlayPauseAsync();
                    break;
                case HotkeyAction.NextTrack:
                    await _musicControlService.NextTrackAsync();
                    break;
                case HotkeyAction.PreviousTrack:
                    await _musicControlService.PreviousTrackAsync();
                    break;
                case HotkeyAction.VolumeUp:
                    await _musicControlService.VolumeUpAsync();
                    break;
                case HotkeyAction.VolumeDown:
                    await _musicControlService.VolumeDownAsync();
                    break;
                case HotkeyAction.MuteUnmute:
                    await _musicControlService.MuteAsync();
                    break;
                case HotkeyAction.SaveTrack:
                    await _musicControlService.SaveTrackAsync();
                    break;
                case HotkeyAction.RemoveTrack:
                    await _musicControlService.RemoveTrackAsync();
                    break;
                case HotkeyAction.ToggleShuffle:
                    await _musicControlService.ToggleShuffleAsync();
                    break;
                case HotkeyAction.ToggleRepeat:
                    await _musicControlService.ToggleRepeatAsync();
                    break;
                case HotkeyAction.SeekForward:
                    await _musicControlService.SeekForwardAsync();
                    break;
                case HotkeyAction.SeekBackward:
                    await _musicControlService.SeekBackwardAsync();
                    break;
                default:
                    _logger.LogWarning("Unknown hotkey action: {Action}", action);
                    break;
            }
        }

        public void Dispose()
        {
            _hotkeyService.UnregisterAllHotkeys();
            _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        }
    }
}
