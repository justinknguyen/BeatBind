using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class MusicControlApplicationService
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MusicControlApplicationService> _logger;
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
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState == null)
                {
                    _logger.LogInformation("No active playback, attempting to start playback");
                    return await _spotifyService.PlayAsync();
                }

                return playbackState.IsPlaying
                    ? await _spotifyService.PauseAsync()
                    : await _spotifyService.PlayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle play/pause");
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
                return await _spotifyService.NextTrackAsync();
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

                if (config.PreviousTrackRewindToStart)
                {
                    var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                    if (playbackState != null && playbackState.ProgressMs > 5000) // 5 seconds
                    {
                        // Rewind to start of current track
                        return await _spotifyService.SeekToPositionAsync(0);
                    }
                }

                return await _spotifyService.PreviousTrackAsync();
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
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    var newVolume = Math.Min(100, playbackState.Volume + config.VolumeSteps);
                    return await _spotifyService.SetVolumeAsync(newVolume);
                }
                return false;
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
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    var newVolume = Math.Max(0, playbackState.Volume - config.VolumeSteps);
                    return await _spotifyService.SetVolumeAsync(newVolume);
                }
                return false;
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
        public async Task<bool> MuteAsync()
        {
            try
            {
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    if (playbackState.Volume > 0)
                    {
                        // Muting: save current volume before muting
                        _lastVolume = playbackState.Volume;
                        return await _spotifyService.SetVolumeAsync(0);
                    }
                    else
                    {
                        // Unmuting: restore saved volume (don't update _lastVolume)
                        return await _spotifyService.SetVolumeAsync(_lastVolume);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle mute");
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
                return await _spotifyService.ToggleShuffleAsync();
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
                return await _spotifyService.ToggleRepeatAsync();
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
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    var newPosition = Math.Min(playbackState.DurationMs, playbackState.ProgressMs + config.SeekMilliseconds);
                    return await _spotifyService.SeekToPositionAsync(newPosition);
                }
                return false;
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
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    var newPosition = Math.Max(0, playbackState.ProgressMs - config.SeekMilliseconds);
                    return await _spotifyService.SeekToPositionAsync(newPosition);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seek backward");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the current playback state including track and device information.
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
    }
}
