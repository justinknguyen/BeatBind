using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class MusicControlService
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<MusicControlService> _logger;
        private int _lastVolume = 50;

        public MusicControlService(ISpotifyService spotifyService, IConfigurationService configurationService, ILogger<MusicControlService> logger)
        {
            _spotifyService = spotifyService;
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<bool> PlayPauseAsync()
        {
            try
            {
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState == null)
                {
                    _logger.LogWarning("No playback state available");
                    return false;
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

        public async Task<bool> MuteAsync()
        {
            try
            {
                var playbackState = await _spotifyService.GetCurrentPlaybackAsync();
                if (playbackState != null)
                {
                    if (playbackState.Volume > 0)
                    {
                        _lastVolume = playbackState.Volume;
                        return await _spotifyService.SetVolumeAsync(0);
                    }
                    else
                    {
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
