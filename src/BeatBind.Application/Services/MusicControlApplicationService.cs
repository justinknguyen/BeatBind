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
        private static readonly TimeSpan PlaybackCacheDuration = TimeSpan.FromSeconds(2);

        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MusicControlApplicationService> _logger;

        // Serializes read-modify-write command sequences so rapid presses compute
        // successive steps (e.g. 50 -> 60 -> 70) instead of racing on the same base value.
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

                    var success = playbackState.IsPlaying
                        ? await _spotifyService.PauseAsync()
                        : await _spotifyService.PlayAsync();

                    if (success)
                    {
                        playbackState.IsPlaying = !playbackState.IsPlaying;
                    }

                    return success;
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
        public async Task<bool> PlayAsync()
        {
            try
            {
                var result = await _spotifyService.PlayAsync();
                if (result)
                {
                    InvalidatePlaybackCache();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start playback");
                return false;
            }
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> PauseAsync()
        {
            try
            {
                var result = await _spotifyService.PauseAsync();
                if (result)
                {
                    InvalidatePlaybackCache();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause playback");
                return false;
            }
        }

        /// <summary>
        /// Skips to the next track in the playback queue.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> NextTrackAsync()
        {
            try
            {
                var result = await _spotifyService.NextTrackAsync();
                if (result)
                {
                    InvalidatePlaybackCache();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to skip to next track");
                return false;
            }
        }

        /// <summary>
        /// Skips to the previous track or rewinds to the start of the current track based on configuration.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> PreviousTrackAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                await _playbackLock.WaitAsync();
                try
                {
                    if (config.PreviousTrackRewindToStart)
                    {
                        var playbackState = await GetPlaybackStateCachedAsync();
                        if (playbackState != null && playbackState.ProgressMs > 5000) // 5 seconds
                        {
                            // Rewind to start of current track
                            if (await _spotifyService.SeekToPositionAsync(0))
                            {
                                playbackState.ProgressMs = 0;
                                return true;
                            }
                            return false;
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
        public async Task<bool> VolumeUpAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return false;
                    }

                    var newVolume = Math.Min(100, playbackState.Volume + config.VolumeSteps);
                    if (!await _spotifyService.SetVolumeAsync(newVolume))
                    {
                        return false;
                    }

                    playbackState.Volume = newVolume;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to increase volume");
                return false;
            }
        }

        /// <summary>
        /// Decreases the playback volume by the configured volume step amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> VolumeDownAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return false;
                    }

                    var newVolume = Math.Max(0, playbackState.Volume - config.VolumeSteps);
                    if (!await _spotifyService.SetVolumeAsync(newVolume))
                    {
                        return false;
                    }

                    playbackState.Volume = newVolume;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrease volume");
                return false;
            }
        }

        /// <summary>
        /// Toggles mute state by setting volume to 0 or restoring the previous volume level.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> ToggleMuteAsync()
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

                    int targetVolume;
                    if (playbackState.Volume > 0)
                    {
                        // Muting: save current volume before muting
                        _lastVolume = playbackState.Volume;
                        targetVolume = 0;
                    }
                    else
                    {
                        // Unmuting: restore saved volume (don't update _lastVolume)
                        targetVolume = _lastVolume;
                    }

                    if (!await _spotifyService.SetVolumeAsync(targetVolume))
                    {
                        return false;
                    }

                    playbackState.Volume = targetVolume;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle mute");
                return false;
            }
        }

        /// <summary>
        /// Mutes the volume by setting it to 0.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> MuteAsync()
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null || playbackState.Volume <= 0)
                    {
                        return false;
                    }

                    _lastVolume = playbackState.Volume;
                    if (!await _spotifyService.SetVolumeAsync(0))
                    {
                        return false;
                    }

                    playbackState.Volume = 0;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mute");
                return false;
            }
        }

        /// <summary>
        /// Unmutes the volume by restoring the previous volume level.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> UnmuteAsync()
        {
            try
            {
                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null || playbackState.Volume != 0)
                    {
                        return false;
                    }

                    if (!await _spotifyService.SetVolumeAsync(_lastVolume))
                    {
                        return false;
                    }

                    playbackState.Volume = _lastVolume;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unmute");
                return false;
            }
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
        public async Task<bool> ToggleShuffleAsync()
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

                    var newState = !playbackState.ShuffleState;
                    if (!await _spotifyService.SetShuffleAsync(newState))
                    {
                        return false;
                    }

                    playbackState.ShuffleState = newState;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle shuffle");
                return false;
            }
        }

        /// <summary>
        /// Cycles through repeat modes: Off -> Context -> Track -> Off.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> ToggleRepeatAsync()
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
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle repeat");
                return false;
            }
        }

        /// <summary>
        /// Seeks forward in the current track by the configured seek milliseconds amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> SeekForwardAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return false;
                    }

                    var newPosition = Math.Min(playbackState.DurationMs, playbackState.ProgressMs + config.SeekMilliseconds);
                    if (!await _spotifyService.SeekToPositionAsync(newPosition))
                    {
                        return false;
                    }

                    playbackState.ProgressMs = newPosition;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seek forward");
                return false;
            }
        }

        /// <summary>
        /// Seeks backward in the current track by the configured seek milliseconds amount.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public async Task<bool> SeekBackwardAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                await _playbackLock.WaitAsync();
                try
                {
                    var playbackState = await GetPlaybackStateCachedAsync();
                    if (playbackState == null)
                    {
                        return false;
                    }

                    var newPosition = Math.Max(0, playbackState.ProgressMs - config.SeekMilliseconds);
                    if (!await _spotifyService.SeekToPositionAsync(newPosition))
                    {
                        return false;
                    }

                    playbackState.ProgressMs = newPosition;
                    return true;
                }
                finally
                {
                    _playbackLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seek backward");
                return false;
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
        /// Returns the cached playback state if it is still fresh; otherwise fetches
        /// a new one from the API. Must be called while holding the playback lock.
        /// Successful commands update the cached state optimistically, so ProgressMs
        /// may lag real playback by up to the cache duration.
        /// </summary>
        /// <returns>The playback state, or null if unavailable.</returns>
        private async Task<PlaybackState?> GetPlaybackStateCachedAsync()
        {
            if (_cachedPlayback != null && DateTime.UtcNow - _cachedPlaybackAtUtc < PlaybackCacheDuration)
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
        /// or otherwise make the cached snapshot unreliable.
        /// </summary>
        private void InvalidatePlaybackCache()
        {
            _cachedPlayback = null;
        }
    }
}
