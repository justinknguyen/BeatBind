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

        /// <summary>
        /// Initializes a new instance of the HotkeyApplicationService class.
        /// </summary>
        /// <param name="hotkeyService">The hotkey service for key registration.</param>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="musicControlService">The music control service for executing actions.</param>
        /// <param name="logger">The logger instance.</param>
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

        /// <summary>
        /// Initializes and registers all enabled hotkeys from the configuration.
        /// </summary>
        public void InitializeHotkeys()
        {
            var hotkeys = _configurationService.GetHotkeys();
            foreach (var hotkey in hotkeys.Where(h => h.IsEnabled))
            {
                RegisterHotkey(hotkey);
            }
        }

        /// <summary>
        /// Registers a hotkey with the underlying hotkey service.
        /// </summary>
        /// <param name="hotkey">The hotkey to register.</param>
        /// <returns>True if registration was successful; otherwise, false.</returns>
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

        /// <summary>
        /// Unregisters a hotkey by its ID.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey to unregister.</param>
        /// <returns>True if unregistration was successful; otherwise, false.</returns>
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

        /// <summary>
        /// Adds a new hotkey to the configuration and registers it if enabled.
        /// </summary>
        /// <param name="hotkey">The hotkey to add.</param>
        public void AddHotkey(Hotkey hotkey)
        {
            _configurationService.AddHotkey(hotkey);
            if (hotkey.IsEnabled)
            {
                RegisterHotkey(hotkey);
            }
        }

        /// <summary>
        /// Removes a hotkey from the configuration and unregisters it.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey to remove.</param>
        public void RemoveHotkey(int hotkeyId)
        {
            UnregisterHotkey(hotkeyId);
            _configurationService.RemoveHotkey(hotkeyId);
        }

        /// <summary>
        /// Updates an existing hotkey, unregistering the old version and registering the new one if enabled.
        /// </summary>
        /// <param name="hotkey">The hotkey with updated values.</param>
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

        /// <summary>
        /// Reloads all hotkeys by unregistering current ones and re-initializing from configuration.
        /// </summary>
        public void ReloadHotkeys()
        {
            // Unregister all current hotkeys
            _hotkeyService.UnregisterAllHotkeys();

            // Re-initialize from configuration
            InitializeHotkeys();
        }

        /// <summary>
        /// Handles the HotkeyPressed event from the hotkey service.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="hotkey">The hotkey that was pressed.</param>
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

        /// <summary>
        /// Creates an Action delegate for the specified hotkey action.
        /// </summary>
        /// <param name="hotkeyAction">The hotkey action to create an Action for.</param>
        /// <returns>An Action delegate that executes the hotkey action.</returns>
        private Action GetActionForHotkey(HotkeyAction hotkeyAction)
        {
            return async () => await ExecuteHotkeyAction(hotkeyAction);
        }

        /// <summary>
        /// Executes the specified hotkey action by calling the appropriate music control service method.
        /// </summary>
        /// <param name="action">The hotkey action to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
