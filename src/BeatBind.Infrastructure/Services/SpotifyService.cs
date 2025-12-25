using System.Text;
using System.Text.Json;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpotifyService> _logger;
        private readonly IAuthenticationService _authenticationService;
        
        private AuthenticationResult? _currentAuth;

        public SpotifyService(
            HttpClient httpClient,
            ILogger<SpotifyService> logger,
            IAuthenticationService authenticationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authenticationService = authenticationService;
            
            // Try to load stored authentication on startup
            LoadStoredAuthentication();
        }

        private void LoadStoredAuthentication()
        {
            try
            {
                var storedAuth = _authenticationService.GetStoredAuthentication();
                if (storedAuth != null && _authenticationService.IsTokenValid(storedAuth))
                {
                    _currentAuth = storedAuth;
                    _logger.LogInformation("Loaded valid stored authentication");
                }
                else if (storedAuth != null && !string.IsNullOrEmpty(storedAuth.RefreshToken))
                {
                    // Try to refresh the token if we have a refresh token
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var refreshedAuth = await _authenticationService.RefreshTokenAsync(storedAuth.RefreshToken);
                            if (refreshedAuth.Success)
                            {
                                _currentAuth = refreshedAuth;
                                _authenticationService.SaveAuthentication(refreshedAuth);
                                _logger.LogInformation("Successfully refreshed stored authentication");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to refresh stored authentication");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stored authentication");
            }
        }

        public bool IsAuthenticated => _currentAuth != null && _authenticationService.IsTokenValid(_currentAuth);

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                _currentAuth = await _authenticationService.AuthenticateAsync();
                
                if (_currentAuth.Success && !string.IsNullOrEmpty(_currentAuth.AccessToken))
                {
                    // Save the authentication tokens for future use
                    _authenticationService.SaveAuthentication(_currentAuth);
                    _logger.LogInformation("Successfully authenticated with Spotify and saved tokens");
                    return true;
                }
                else
                {
                    _logger.LogError("Authentication failed: {Error}", _currentAuth.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (_currentAuth == null || string.IsNullOrEmpty(_currentAuth.RefreshToken))
                {
                    return false;
                }

                _currentAuth = await _authenticationService.RefreshTokenAsync(_currentAuth.RefreshToken);
                
                if (_currentAuth.Success)
                {
                    // Save the refreshed tokens
                    _authenticationService.SaveAuthentication(_currentAuth);
                    _logger.LogInformation("Successfully refreshed and saved authentication tokens");
                }
                
                return _currentAuth.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return false;
            }
        }

        public async Task<PlaybackState?> GetCurrentPlaybackAsync()
        {
            try
            {
                if (!await EnsureValidTokenAsync()) return null;

                var url = "https://api.spotify.com/v1/me/player";
                _logger.LogInformation("GET {Url}", url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuth!.AccessToken);

                var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null; // No active device
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    return ParsePlaybackState(root);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current playback");
                return null;
            }
        }

        public async Task<bool> PlayAsync()
        {
            return await SendPlayerCommandAsync("play", HttpMethod.Put);
        }

        public async Task<bool> PauseAsync()
        {
            return await SendPlayerCommandAsync("pause", HttpMethod.Put);
        }

        public async Task<bool> NextTrackAsync()
        {
            return await SendPlayerCommandAsync("next", HttpMethod.Post);
        }

        public async Task<bool> PreviousTrackAsync()
        {
            return await SendPlayerCommandAsync("previous", HttpMethod.Post);
        }

        public async Task<bool> SetVolumeAsync(int volume)
        {
            try
            {
                if (!await EnsureValidTokenAsync()) return false;

                volume = Math.Clamp(volume, 0, 100);
                var url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}";
                _logger.LogInformation("PUT {Url}", url);
                
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuth!.AccessToken);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting volume");
                return false;
            }
        }

        public async Task<bool> ToggleShuffleAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback == null) return false;

                var newShuffleState = !playback.ShuffleState;
                var url = $"https://api.spotify.com/v1/me/player/shuffle?state={newShuffleState.ToString().ToLower()}";
                
                return await SendPlayerCommandAsync(url, HttpMethod.Put, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling shuffle");
                return false;
            }
        }

        public async Task<bool> ToggleRepeatAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback == null) return false;

                var newRepeatState = playback.RepeatState switch
                {
                    RepeatMode.Off => "context",
                    RepeatMode.Context => "track",
                    RepeatMode.Track => "off",
                    _ => "off"
                };

                var url = $"https://api.spotify.com/v1/me/player/repeat?state={newRepeatState}";
                return await SendPlayerCommandAsync(url, HttpMethod.Put, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling repeat");
                return false;
            }
        }

        public async Task<bool> SaveCurrentTrackAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback?.CurrentTrack == null) return false;

                var url = "https://api.spotify.com/v1/me/tracks";
                var json = JsonSerializer.Serialize(new { ids = new[] { playback.CurrentTrack.Id } });
                
                return await SendPlayerCommandAsync(url, HttpMethod.Put, json, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving track");
                return false;
            }
        }

        public async Task<bool> RemoveCurrentTrackAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback?.CurrentTrack == null) return false;

                var url = "https://api.spotify.com/v1/me/tracks";
                var json = JsonSerializer.Serialize(new { ids = new[] { playback.CurrentTrack.Id } });
                
                return await SendPlayerCommandAsync(url, HttpMethod.Delete, json, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing track");
                return false;
            }
        }

        public async Task<bool> SeekToPositionAsync(int positionMs)
        {
            try
            {
                if (!await EnsureValidTokenAsync()) return false;

                positionMs = Math.Max(0, positionMs);
                var url = $"https://api.spotify.com/v1/me/player/seek?position_ms={positionMs}";
                _logger.LogInformation("PUT {Url}", url);
                
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuth!.AccessToken);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeking to position");
                return false;
            }
        }

        private async Task<bool> EnsureValidTokenAsync()
        {
            if (_currentAuth == null)
            {
                return await AuthenticateAsync();
            }

            if (!_authenticationService.IsTokenValid(_currentAuth))
            {
                return await RefreshTokenAsync();
            }

            return true;
        }

        private async Task<bool> SendPlayerCommandAsync(string endpoint, HttpMethod method, string? body = null, bool useFullUrl = false)
        {
            try
            {
                if (!await EnsureValidTokenAsync()) return false;

                var url = useFullUrl ? endpoint : $"https://api.spotify.com/v1/me/player/{endpoint}";
                _logger.LogInformation("{Method} {Url}", method.Method, url);
                var request = new HttpRequestMessage(method, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuth!.AccessToken);

                if (!string.IsNullOrEmpty(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending player command: {Endpoint}", endpoint);
                return false;
            }
        }

        private static PlaybackState ParsePlaybackState(JsonElement root)
        {
            var playbackState = new PlaybackState
            {
                IsPlaying = root.GetProperty("is_playing").GetBoolean(),
                ShuffleState = root.GetProperty("shuffle_state").GetBoolean(),
                Volume = root.GetProperty("device").GetProperty("volume_percent").GetInt32(),
                ProgressMs = root.GetProperty("progress_ms").GetInt32()
            };

            // Parse repeat state
            var repeatState = root.GetProperty("repeat_state").GetString();
            playbackState.RepeatState = repeatState switch
            {
                "off" => RepeatMode.Off,
                "track" => RepeatMode.Track,
                "context" => RepeatMode.Context,
                _ => RepeatMode.Off
            };

            // Parse current track
            if (root.TryGetProperty("item", out var item))
            {
                var durationMs = item.GetProperty("duration_ms").GetInt32();
                playbackState.DurationMs = durationMs;
                
                playbackState.CurrentTrack = new Track
                {
                    Id = item.GetProperty("id").GetString() ?? string.Empty,
                    Name = item.GetProperty("name").GetString() ?? string.Empty,
                    Uri = item.GetProperty("uri").GetString() ?? string.Empty,
                    DurationMs = durationMs,
                    Artist = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? string.Empty,
                    Album = item.GetProperty("album").GetProperty("name").GetString() ?? string.Empty,
                    IsPlaying = playbackState.IsPlaying,
                    ProgressMs = playbackState.ProgressMs
                };
            }

            // Parse device
            if (root.TryGetProperty("device", out var device))
            {
                playbackState.Device = new Device
                {
                    Id = device.GetProperty("id").GetString() ?? string.Empty,
                    Name = device.GetProperty("name").GetString() ?? string.Empty,
                    Type = device.GetProperty("type").GetString() ?? string.Empty,
                    IsActive = device.GetProperty("is_active").GetBoolean(),
                    IsPrivateSession = device.GetProperty("is_private_session").GetBoolean(),
                    IsRestricted = device.GetProperty("is_restricted").GetBoolean(),
                    VolumePercent = device.GetProperty("volume_percent").GetInt32()
                };
            }

            return playbackState;
        }
    }
}
