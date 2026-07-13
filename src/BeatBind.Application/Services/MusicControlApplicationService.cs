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
                        return await _spotifyService.PlayAsync();
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
                        return await _spotifyService.PlayAsync();
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
            return ExecuteInvalidatingCommandAsync("start playback", _spotifyService.PlayAsync);
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
        /// Sends play or pause based on the given state and flips the cached
        /// IsPlaying flag on success.
        /// </summary>
        private async Task<bool> TogglePlaybackAsync(PlaybackState playbackState)
        {
            var success = playbackState.IsPlaying
                ? await _spotifyService.PauseAsync()
                : await _spotifyService.PlayAsync();

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
