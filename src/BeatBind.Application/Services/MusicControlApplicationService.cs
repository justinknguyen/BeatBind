using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class MusicControlApplicationService
    {
        // How long a fetched playback state stays fresh. Within this window,
        // repeated hotkey presses (e.g. mashing volume up) reuse the cached state
        // and send only the command request, halving the round trips per press.
        private static readonly TimeSpan _playbackCacheDuration = TimeSpan.FromSeconds(2);

        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MusicControlApplicationService> _logger;

        // Serializes command sequences so rapid presses compute successive steps
        // (e.g. 50 -> 60 -> 70) instead of racing on the same base value. Every
        // read or write of _cachedPlayback must happen while holding this lock.
        private readonly SemaphoreSlim _playbackLock = new(1, 1);
        private PlaybackState? _cachedPlayback;
        private DateTime _cachedPlaybackAtUtc;
        private int _lastVolume = 50;

        /// <summary>
        /// Initializes a new instance of the MusicControlApplicationService class.
        /// </summary>
        /// <param name="spotifyService">The Spotify service for music control.</param>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="logger">The logger instance.</param>
        public MusicControlApplicationService(ISpotifyService spotifyService, IConfigurationService configurationService, ILogger<MusicControlApplicationService> logger)
        {
            _spotifyService = spotifyService;
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Toggles between play and pause states based on current playback state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> PlayPauseAsync()
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        _logger.LogInformation("No active playback, attempting to start playback");
                        return await _spotifyService.PlayAsync(GetFavoriteDeviceId());
                    }

                    if (await TogglePlaybackAsync(playbackState))
                    {
                        return true;
                    }

                    // The command can fail because the cached state is stale — e.g.
                    // playback ended on its own, so pausing was rejected. Refetch
                    // the real state and retry once
                    InvalidatePlaybackCache();
                    playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return await _spotifyService.PlayAsync(GetFavoriteDeviceId());
                    }

                    return await TogglePlaybackAsync(playbackState);
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle play/pause");
                return false;
            }
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> PlayAsync()
        {
            return ExecuteInvalidatingCommandAsync("start playback", () => _spotifyService.PlayAsync(GetFavoriteDeviceId()));
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> PauseAsync()
        {
            return ExecuteInvalidatingCommandAsync("pause playback", _spotifyService.PauseAsync);
        }

        /// <summary>
        /// Skips to the next track in the playback queue.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> NextTrackAsync()
        {
            return ExecuteInvalidatingCommandAsync("skip to next track", _spotifyService.NextTrackAsync);
        }

        /// <summary>
        /// Skips to the previous track or rewinds to the start of the current track based on configuration.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> PreviousTrackAsync()
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var config = _configurationService.GetConfiguration();
                    if (config.PreviousTrackRewindToStart)
                    {
                        var playbackState = await GetPlaybackStateCachedAsync();
                        if (playbackState != null && playbackState.ProgressMs > 5000) // 5 seconds
                        {
                            // Rewind to start of current track
                            if (!await _spotifyService.SeekToPositionAsync(0))
                            {
                                return false;
                            }

                            playbackState.ProgressMs = 0;
                            return true;
                        }
                    }

                    var result = await _spotifyService.PreviousTrackAsync();
                    if (result)
                    {
                        InvalidatePlaybackCache();
                    }
                    return result;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to skip to previous track");
                return false;
            }
        }

        /// <summary>
        /// Increases the playback volume by the configured volume step amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> VolumeUpAsync()
        {
            return AdjustVolumeAsync("increase volume", direction: +1);
        }

        /// <summary>
        /// Decreases the playback volume by the configured volume step amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> VolumeDownAsync()
        {
            return AdjustVolumeAsync("decrease volume", direction: -1);
        }

        /// <summary>
        /// Toggles mute state by setting volume to 0 or restoring the previous volume level.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> ToggleMuteAsync()
        {
            return ExecuteWithPlaybackStateAsync("toggle mute", async playbackState =>
            {
                if (playbackState.Volume is not int volume)
                {
                    return false;
                }

                if (volume > 0)
                {
                    // Muting: save current volume before muting
                    _lastVolume = volume;
                    return await SetVolumeAndCacheAsync(playbackState, 0);
                }

                // Unmuting: restore saved volume (don't update _lastVolume)
                return await SetVolumeAndCacheAsync(playbackState, _lastVolume);
            });
        }

        /// <summary>
        /// Mutes the volume by setting it to 0.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> MuteAsync()
        {
            return ExecuteWithPlaybackStateAsync("mute", async playbackState =>
            {
                if (playbackState.Volume is not int volume || volume <= 0)
                {
                    return false;
                }

                _lastVolume = volume;
                return await SetVolumeAndCacheAsync(playbackState, 0);
            });
        }

        /// <summary>
        /// Unmutes the volume by restoring the previous volume level.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> UnmuteAsync()
        {
            return ExecuteWithPlaybackStateAsync("unmute", async playbackState =>
            {
                if (playbackState.Volume != 0)
                {
                    return false;
                }

                return await SetVolumeAndCacheAsync(playbackState, _lastVolume);
            });
        }

        /// <summary>
        /// Saves the currently playing track to the user's Spotify library.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> SaveTrackAsync()
        {
            try
            {
                return await _spotifyService.SaveCurrentTrackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save track");
                return false;
            }
        }

        /// <summary>
        /// Removes the currently playing track from the user's Spotify library.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> RemoveTrackAsync()
        {
            try
            {
                return await _spotifyService.RemoveCurrentTrackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove track");
                return false;
            }
        }

        /// <summary>
        /// Toggles shuffle mode for the current playback.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> ToggleShuffleAsync()
        {
            return ExecuteWithPlaybackStateAsync("toggle shuffle", async playbackState =>
            {
                var newState = !playbackState.ShuffleState;
                if (!await _spotifyService.SetShuffleAsync(newState))
                {
                    return false;
                }

                playbackState.ShuffleState = newState;
                return true;
            });
        }

        /// <summary>
        /// Cycles through repeat modes: Off -> Context -> Track -> Off.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> ToggleRepeatAsync()
        {
            return ExecuteWithPlaybackStateAsync("toggle repeat", async playbackState =>
            {
                var newMode = playbackState.RepeatState switch
                {
                    RepeatMode.Off => RepeatMode.Context,
                    RepeatMode.Context => RepeatMode.Track,
                    _ => RepeatMode.Off
                };

                if (!await _spotifyService.SetRepeatAsync(newMode))
                {
                    return false;
                }

                playbackState.RepeatState = newMode;
                return true;
            });
        }

        /// <summary>
        /// Seeks forward in the current track by the configured seek milliseconds amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> SeekForwardAsync()
        {
            return SeekByAsync("seek forward", direction: +1);
        }

        /// <summary>
        /// Seeks backward in the current track by the configured seek milliseconds amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public Task<bool> SeekBackwardAsync()
        {
            return SeekByAsync("seek backward", direction: -1);
        }

        /// <summary>
        /// Toggles playback on the configured favorite device. When music is already
        /// playing there this simply pauses; otherwise playback is transferred to the
        /// favorite and started, so resuming after a long pause lands on the user's
        /// chosen speakers instead of wherever Spotify last left off.
        /// Falls back to the normal play/pause behavior when no favorite is
        /// configured or the favorite is not currently available.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> PlayPauseOnFavoriteDeviceAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                var favoriteId = NormalizeDeviceValue(config.FavoriteDeviceId);
                var favoriteName = NormalizeDeviceValue(config.FavoriteDeviceName);

                if (favoriteId == null && favoriteName == null)
                {
                    _logger.LogInformation("No favorite device configured, falling back to play/pause");
                    return await PlayPauseAsync();
                }

                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState != null
                        && playbackState.IsPlaying
                        && IsFavoriteDevice(playbackState.Device, favoriteId, favoriteName))
                    {
                        // Already playing where the user wants it, so behave as a toggle
                        if (!await _spotifyService.PauseAsync())
                        {
                            return false;
                        }

                        playbackState.IsPlaying = false;
                        return true;
                    }

                    var outcome = await TransferToFavoriteAsync(favoriteId, favoriteName);
                    if (outcome == FavoriteTransferOutcome.Transferred)
                    {
                        InvalidatePlaybackCache();
                        return true;
                    }

                    if (outcome == FavoriteTransferOutcome.TransferFailed)
                    {
                        // The device is there but Spotify rejected the transfer (rate limit,
                        // transient error). Leave playback alone: pausing what the user is
                        // listening to is a worse outcome than doing nothing.
                        _logger.LogWarning(
                            "Transfer to favorite device {DeviceName} failed, leaving playback unchanged",
                            favoriteName ?? favoriteId);
                        return false;
                    }

                    // The favorite is asleep, powered off, or otherwise not visible to
                    // Spotify — fall back to normal play/pause so the hotkey still does
                    // something rather than looking dead
                    _logger.LogWarning(
                        "Favorite device {DeviceName} is unavailable, falling back to normal play/pause",
                        favoriteName ?? favoriteId);

                    if (playbackState == null)
                    {
                        return await _spotifyService.PlayAsync(favoriteId);
                    }

                    return await TogglePlaybackAsync(playbackState);
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play/pause on favorite device");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the Spotify devices currently visible to the account, so the UI
        /// can offer them when picking a favorite device.
        /// </summary>
        /// <returns>The available devices, or an empty list if none could be retrieved.</returns>
        public async Task<List<Device>> GetAvailableDevicesAsync()
        {
            try
            {
                return await _spotifyService.GetAvailableDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available devices");
                return new List<Device>();
            }
        }

        /// <summary>
        /// Retrieves the current playback state including track and device information.
        /// Always queries the API so UI consumers see fresh data.
        /// </summary>
        /// <returns>The current playback state, or null if unavailable.</returns>
        public async Task<PlaybackState?> GetCurrentPlaybackAsync()
        {
            try
            {
                return await _spotifyService.GetCurrentPlaybackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current playback state");
                return null;
            }
        }

        /// <summary>
        /// Outcome of trying to move playback to the favorite device. The caller needs to
        /// tell "the device is gone" apart from "the request failed", because only the
        /// former justifies disturbing whatever is currently playing.
        /// </summary>
        private enum FavoriteTransferOutcome
        {
            /// <summary>Playback is now on the favorite device.</summary>
            Transferred,

            /// <summary>Spotify does not currently list the favorite device.</summary>
            DeviceUnavailable,

            /// <summary>The device is listed, but the transfer request failed.</summary>
            TransferFailed
        }

        /// <summary>
        /// Moves playback to the favorite device. The stored id is tried first so the
        /// common case costs a single request; on failure the device list decides whether
        /// the device is genuinely absent or the request merely failed, and doubles as
        /// name-based recovery for when Spotify rotates the device id.
        /// </summary>
        private async Task<FavoriteTransferOutcome> TransferToFavoriteAsync(string? favoriteId, string? favoriteName)
        {
            if (favoriteId != null && await _spotifyService.TransferPlaybackAsync(favoriteId))
            {
                return FavoriteTransferOutcome.Transferred;
            }

            var devices = await _spotifyService.GetAvailableDevicesAsync();
            var match = devices.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.Id) && IsFavoriteDevice(d, favoriteId, favoriteName));

            if (match == null)
            {
                return FavoriteTransferOutcome.DeviceUnavailable;
            }

            if (!string.Equals(match.Id, favoriteId, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Favorite device id changed, re-resolved {DeviceName} to {DeviceId}",
                    match.Name,
                    match.Id);
            }

            // Retries the same id when the first attempt failed but the device is still
            // listed, which covers a transient rejection
            return await _spotifyService.TransferPlaybackAsync(match.Id)
                ? FavoriteTransferOutcome.Transferred
                : FavoriteTransferOutcome.TransferFailed;
        }

        /// <summary>
        /// Matches a device against the favorite, by id and then by name so a rotated
        /// device id still counts as the same speaker. Matching on name means two devices
        /// sharing a name (Spotify readily reports several "Web Player" entries) are
        /// treated as interchangeable; that is accepted deliberately, because id rotation
        /// is the common case and telling twins apart would cost an extra request on
        /// every keypress.
        /// </summary>
        private static bool IsFavoriteDevice(Device? device, string? favoriteId, string? favoriteName)
        {
            if (device == null)
            {
                return false;
            }

            if (favoriteId != null && string.Equals(device.Id, favoriteId, StringComparison.Ordinal))
            {
                return true;
            }

            return favoriteName != null && string.Equals(device.Name, favoriteName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads the configured favorite device id, or null when none is set.
        /// </summary>
        private string? GetFavoriteDeviceId()
        {
            return NormalizeDeviceValue(_configurationService.GetConfiguration().FavoriteDeviceId);
        }

        /// <summary>
        /// Treats blank configuration values as absent. Null (rather than empty) is what
        /// ISpotifyService reads as "no device preference".
        /// </summary>
        private static string? NormalizeDeviceValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Sends play or pause based on the given state and flips the cached
        /// IsPlaying flag on success.
        /// </summary>
        private async Task<bool> TogglePlaybackAsync(PlaybackState playbackState)
        {
            var success = playbackState.IsPlaying
                ? await _spotifyService.PauseAsync()
                : await _spotifyService.PlayAsync(GetFavoriteDeviceId());

            if (success)
            {
                playbackState.IsPlaying = !playbackState.IsPlaying;
            }

            return success;
        }

        /// <summary>
        /// Changes the volume by the configured step in the given direction.
        /// Does nothing when the device does not report its volume — an absolute
        /// step from an assumed level could yank the real volume wildly.
        /// </summary>
        private Task<bool> AdjustVolumeAsync(string operation, int direction)
        {
            return ExecuteWithPlaybackStateAsync(operation, async playbackState =>
            {
                if (playbackState.Volume is not int volume)
                {
                    return false;
                }

                var steps = _configurationService.GetConfiguration().VolumeSteps;
                var newVolume = Math.Clamp(volume + direction * steps, 0, 100);
                return await SetVolumeAndCacheAsync(playbackState, newVolume);
            });
        }

        /// <summary>
        /// Seeks by the configured amount in the given direction, clamped to the track bounds.
        /// </summary>
        private Task<bool> SeekByAsync(string operation, int direction)
        {
            return ExecuteWithPlaybackStateAsync(operation, async playbackState =>
            {
                var seekMs = _configurationService.GetConfiguration().SeekMilliseconds;
                var newPosition = Math.Clamp(playbackState.ProgressMs + direction * seekMs, 0, playbackState.DurationMs);
                if (!await _spotifyService.SeekToPositionAsync(newPosition))
                {
                    return false;
                }

                playbackState.ProgressMs = newPosition;
                return true;
            });
        }

        /// <summary>
        /// Sets the volume and mirrors the new value into the cached playback state on success.
        /// </summary>
        private async Task<bool> SetVolumeAndCacheAsync(PlaybackState playbackState, int targetVolume)
        {
            if (!await _spotifyService.SetVolumeAsync(targetVolume))
            {
                return false;
            }

            playbackState.Volume = targetVolume;
            return true;
        }

        /// <summary>
        /// Runs an action against the (possibly cached) playback state while holding
        /// the playback lock; returns false when no playback state is available.
        /// Exceptions are logged with the operation name and reported as failure.
        /// </summary>
        private async Task<bool> ExecuteWithPlaybackStateAsync(string operation, Func<PlaybackState, Task<bool>> action)
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return false;
                    }

                    return await action(playbackState);
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Operation}", operation);
                return false;
            }
        }

        /// <summary>
        /// Runs a command that changes what is playing, invalidating the cached
        /// playback state on success. Holds the playback lock so the invalidation
        /// cannot race a concurrent cached read.
        /// </summary>
        private async Task<bool> ExecuteInvalidatingCommandAsync(string operation, Func<Task<bool>> command)
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var result = await command();
                    if (result)
                    {
                        InvalidatePlaybackCache();
                    }
                    return result;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Operation}", operation);
                return false;
            }
        }

        /// <summary>
        /// Returns the cached playback state if it is still fresh; otherwise fetches
        /// a new one from the API. Must be called while holding the playback lock.
        /// Successful commands update the cached state optimistically, so ProgressMs
        /// may lag real playback by up to the cache duration.
        /// </summary>
        /// <returns>The playback state, or null if unavailable.</returns>
        private async Task<PlaybackState?> GetPlaybackStateCachedAsync()
        {
            if (_cachedPlayback != null && DateTime.UtcNow - _cachedPlaybackAtUtc < _playbackCacheDuration)
            {
                return _cachedPlayback;
            }

            var state = await _spotifyService.GetCurrentPlaybackAsync();
            _cachedPlayback = state;
            _cachedPlaybackAtUtc = DateTime.UtcNow;
            return state;
        }

        /// <summary>
        /// Discards the cached playback state after commands that change the track
        /// or otherwise make the cached snapshot unreliable. Must be called while
        /// holding the playback lock.
        /// </summary>
        private void InvalidatePlaybackCache()
        {
            _cachedPlayback = null;
        }
    }
}
