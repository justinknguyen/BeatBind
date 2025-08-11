using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    public class SpotifyBackend
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpotifyBackend> _logger;
        private readonly ConfigurationManager _config;
        private readonly SpotifyOAuthHandler _oAuthHandler;
        
        private string? _accessToken;
        private string? _refreshToken;
        private DateTime _tokenExpiry;
        private int _lastVolume = 50;

        public SpotifyBackend()
        {
            _httpClient = new HttpClient();
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SpotifyBackend>();
            _config = new ConfigurationManager();
            _oAuthHandler = new SpotifyOAuthHandler(_config);
        }

        public bool IsTokenValid => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry;

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var result = await _oAuthHandler.AuthenticateAsync();
                
                if (result.Success && !string.IsNullOrEmpty(result.AccessToken))
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60); // Subtract 60 seconds for safety
                    
                    _logger.LogInformation("Successfully authenticated with Spotify");
                    return true;
                }
                else
                {
                    _logger.LogError($"Authentication failed: {result.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return false;
            }
        }

        public async Task<bool> PlayPauseAsync()
        {
            try
            {
                if (!IsTokenValid) return false;

                var isPlaying = await GetPlaybackStateAsync();
                var endpoint = isPlaying ? "pause" : "play";
                
                return await SendPlayerCommandAsync(endpoint, HttpMethod.Put);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PlayPause");
                return false;
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

        public async Task<bool> AdjustVolumeAsync(int amount)
        {
            try
            {
                if (!IsTokenValid) return false;

                var currentVolume = await GetCurrentVolumeAsync();
                var newVolume = Math.Max(0, Math.Min(100, currentVolume + amount));
                
                var url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={newVolume}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    _lastVolume = newVolume;
                    _logger.LogInformation($"Volume adjusted to {newVolume}%");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting volume");
                return false;
            }
        }

        public async Task<bool> MuteAsync()
        {
            try
            {
                var currentVolume = await GetCurrentVolumeAsync();
                if (currentVolume > 0)
                {
                    _lastVolume = currentVolume;
                    return await SetVolumeAsync(0);
                }
                else
                {
                    return await SetVolumeAsync(_lastVolume);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error muting/unmuting");
                return false;
            }
        }

        public async Task<bool> SeekAsync(int offsetMs)
        {
            try
            {
                if (!IsTokenValid) return false;

                var currentPosition = await GetCurrentPlaybackPositionAsync();
                var newPosition = Math.Max(0, currentPosition + offsetMs);
                
                var url = $"https://api.spotify.com/v1/me/player/seek?position_ms={newPosition}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.PutAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeking");
                return false;
            }
        }

        public async Task<bool> SaveCurrentTrackAsync()
        {
            try
            {
                var trackId = await GetCurrentTrackIdAsync();
                if (string.IsNullOrEmpty(trackId)) return false;

                return await SaveTrackAsync(trackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving current track");
                return false;
            }
        }

        public async Task<bool> RemoveCurrentTrackAsync()
        {
            try
            {
                var trackId = await GetCurrentTrackIdAsync();
                if (string.IsNullOrEmpty(trackId)) return false;

                return await RemoveTrackAsync(trackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing current track");
                return false;
            }
        }

        private async Task<bool> SendPlayerCommandAsync(string endpoint, HttpMethod method)
        {
            try
            {
                if (!IsTokenValid) return false;

                var url = $"https://api.spotify.com/v1/me/player/{endpoint}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var request = new HttpRequestMessage(method, url);
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully executed {endpoint} command");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending {endpoint} command");
                return false;
            }
        }

        private async Task<bool> GetPlaybackStateAsync()
        {
            try
            {
                var playerInfo = await GetPlayerInfoAsync();
                return playerInfo?.GetProperty("is_playing").GetBoolean() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playback state");
                return false;
            }
        }

        private async Task<int> GetCurrentVolumeAsync()
        {
            try
            {
                var playerInfo = await GetPlayerInfoAsync();
                return playerInfo?.GetProperty("device").GetProperty("volume_percent").GetInt32() ?? 50;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current volume");
                return _lastVolume;
            }
        }

        private async Task<int> GetCurrentPlaybackPositionAsync()
        {
            try
            {
                var playerInfo = await GetPlayerInfoAsync();
                return playerInfo?.GetProperty("progress_ms").GetInt32() ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current playback position");
                return 0;
            }
        }

        private async Task<string?> GetCurrentTrackIdAsync()
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player/currently-playing";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                
                return json.RootElement.GetProperty("item").GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current track ID");
                return null;
            }
        }

        private async Task<JsonElement?> GetPlayerInfoAsync()
        {
            try
            {
                if (!IsTokenValid) return null;

                var url = "https://api.spotify.com/v1/me/player";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content)) return null;
                
                var json = JsonDocument.Parse(content);
                return json.RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player info");
                return null;
            }
        }

        private async Task<bool> SetVolumeAsync(int volume)
        {
            try
            {
                if (!IsTokenValid) return false;

                var url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.PutAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting volume");
                return false;
            }
        }

        private async Task<bool> SaveTrackAsync(string trackId)
        {
            try
            {
                if (!IsTokenValid) return false;

                var url = "https://api.spotify.com/v1/me/tracks";
                var data = new { ids = new[] { trackId } };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var response = await _httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Track saved successfully");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving track");
                return false;
            }
        }

        private async Task<bool> RemoveTrackAsync(string trackId)
        {
            try
            {
                if (!IsTokenValid) return false;

                var url = "https://api.spotify.com/v1/me/tracks";
                var data = new { ids = new[] { trackId } };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                
                var request = new HttpRequestMessage(HttpMethod.Delete, url)
                {
                    Content = content
                };
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Track removed successfully");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing track");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _oAuthHandler?.Dispose();
        }
    }
}
