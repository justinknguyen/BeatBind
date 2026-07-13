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

        // Serializes token refreshes so concurrent hotkey presses don't each
        // spend the same refresh token independently.
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private AuthenticationResult? _currentAuth;

        /// <summary>
        /// Initializes a new instance of the SpotifyService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="authenticationService">The authentication service.</param>
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

            if (_currentAuth != null && !_authenticationService.IsTokenValid(_currentAuth))
            {
                // Expired but refreshable: refresh in the background so the first
                // hotkey press doesn't pay for the token round trip. This is safe to
                // race with API calls because EnsureValidTokenAsync serializes with it
                // through the refresh lock.
                _ = Task.Run(() => EnsureValidTokenAsync());
            }
        }

        /// <summary>
        /// Loads stored authentication tokens from storage into the current session.
        /// The tokens may be expired; EnsureValidTokenAsync refreshes them on demand.
        /// </summary>
        private void LoadStoredAuthentication()
        {
            try
            {
                var storedAuth = _authenticationService.GetStoredAuthentication();
                if (storedAuth != null)
                {
                    _currentAuth = storedAuth;
                    if (_authenticationService.IsTokenValid(storedAuth))
                    {
                        _logger.LogInformation("Loaded valid stored authentication");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stored authentication");
            }
        }

        /// <summary>
        /// Indicates whether the user is currently authenticated with valid tokens.
        /// </summary>
        public bool IsAuthenticated => _currentAuth != null && _authenticationService.IsTokenValid(_currentAuth);

        /// <summary>
        /// Authenticates with Spotify using OAuth 2.0 authorization code flow.
        /// Opens a browser window for user authentication and stores the resulting tokens.
        /// </summary>
        /// <returns>True if authentication was successful; otherwise, false.</returns>
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

        /// <summary>
        /// Refreshes the current access token using the stored refresh token.
        /// Saves the new tokens to storage upon successful refresh.
        /// </summary>
        /// <returns>True if the token was successfully refreshed; otherwise, false.</returns>
        public Task<bool> RefreshTokenAsync()
        {
            return RefreshTokenCoreAsync(force: true);
        }

        /// <summary>
        /// Refreshes the access token while holding the refresh lock.
        /// </summary>
        /// <param name="force">When false, skips the refresh if another caller already refreshed the token.</param>
        /// <returns>True if a valid token is available after the call; otherwise, false.</returns>
        private async Task<bool> RefreshTokenCoreAsync(bool force)
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_currentAuth == null || string.IsNullOrEmpty(_currentAuth.RefreshToken))
                {
                    return false;
                }

                if (!force && _authenticationService.IsTokenValid(_currentAuth))
                {
                    // Another caller refreshed while we waited on the lock
                    return true;
                }

                var refreshedAuth = await _authenticationService.RefreshTokenAsync(_currentAuth.RefreshToken);

                if (refreshedAuth != null && refreshedAuth.Success)
                {
                    // Only replace the current tokens on success so a transient failure
                    // doesn't wipe the refresh token and break the session until restart
                    _currentAuth = refreshedAuth;
                    _authenticationService.SaveAuthentication(refreshedAuth);
                    _logger.LogInformation("Successfully refreshed and saved authentication tokens");
                    return true;
                }

                _logger.LogWarning("Token refresh failed: {Error}", refreshedAuth?.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Retrieves the current playback state from Spotify including track, device, and playback information.
        /// </summary>
        /// <returns>The current playback state, or null if no active playback or device is found.</returns>
        public async Task<PlaybackState?> GetCurrentPlaybackAsync()
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player";
                var response = await SendRequestAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
                if (response == null)
                {
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null; // No active device
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(content);
                    return ParsePlaybackState(jsonDoc.RootElement);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current playback");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the list of available Spotify devices.
        /// </summary>
        /// <returns>A list of available devices.</returns>
        public async Task<List<Device>> GetAvailableDevicesAsync()
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player/devices";
                var response = await SendRequestAsync(() => new HttpRequestMessage(HttpMethod.Get, url));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    return new List<Device>();
                }

                var content = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(content);
                var devices = new List<Device>();

                if (jsonDoc.RootElement.TryGetProperty("devices", out var devicesArray))
                {
                    foreach (var deviceElement in devicesArray.EnumerateArray())
                    {
                        devices.Add(ParseDevice(deviceElement));
                    }
                }

                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available devices");
                return new List<Device>();
            }
        }

        /// <summary>
        /// Starts or resumes playback on the active Spotify device.
        /// If no device is active, attempts to transfer playback to an available device.
        /// </summary>
        /// <returns>True if the command was successful; otherwise, false.</returns>
        public async Task<bool> PlayAsync()
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player/play";
                var response = await SendRequestAsync(() => new HttpRequestMessage(HttpMethod.Put, url));
                if (response == null)
                {
                    return false;
                }

                // If we get a 404, it likely means no active device
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No active device found (404), checking for available devices");
                    var devices = await GetAvailableDevicesAsync();

                    if (devices.Count == 0)
                    {
                        _logger.LogWarning("No available devices found. Please open Spotify on a device.");
                        return false;
                    }

                    // Try to transfer playback to the first available device (prefer active ones)
                    var device = devices.FirstOrDefault(d => d.IsActive) ?? devices.First();
                    _logger.LogInformation("Attempting to transfer playback to device: {DeviceName}", device.Name);

                    var transferBody = JsonSerializer.Serialize(new { device_ids = new[] { device.Id }, play = true });
                    var transferUrl = "https://api.spotify.com/v1/me/player";
                    var transferResponse = await SendRequestAsync(() => new HttpRequestMessage(HttpMethod.Put, transferUrl)
                    {
                        Content = new StringContent(transferBody, Encoding.UTF8, "application/json")
                    });

                    if (transferResponse == null || !transferResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to transfer playback. Status: {StatusCode}. Ensure Spotify has recently played content.", transferResponse?.StatusCode);
                        return false;
                    }

                    return true;
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing");
                return false;
            }
        }

        /// <summary>
        /// Pauses playback on the active Spotify device.
        /// </summary>
        /// <returns>True if the command was successful; otherwise, false.</returns>
        public async Task<bool> PauseAsync()
        {
            return await SendPlayerCommandAsync("pause", HttpMethod.Put);
        }

        /// <summary>
        /// Skips to the next track in the playback queue.
        /// </summary>
        /// <returns>True if the command was successful; otherwise, false.</returns>
        public async Task<bool> NextTrackAsync()
        {
            return await SendPlayerCommandAsync("next", HttpMethod.Post);
        }

        /// <summary>
        /// Skips to the previous track in the playback queue.
        /// </summary>
        /// <returns>True if the command was successful; otherwise, false.</returns>
        public async Task<bool> PreviousTrackAsync()
        {
            return await SendPlayerCommandAsync("previous", HttpMethod.Post);
        }

        /// <summary>
        /// Sets the playback volume on the active Spotify device.
        /// Volume is automatically clamped to the range 0-100.
        /// </summary>
        /// <param name="volume">The volume level (0-100).</param>
        /// <returns>True if the volume was successfully set; otherwise, false.</returns>
        public async Task<bool> SetVolumeAsync(int volume)
        {
            volume = Math.Clamp(volume, 0, 100);
            var url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}";
            return await SendPlayerCommandAsync(url, HttpMethod.Put, useFullUrl: true);
        }

        /// <summary>
        /// Toggles the shuffle mode for the current playback.
        /// </summary>
        /// <returns>True if shuffle mode was successfully toggled; otherwise, false.</returns>
        public async Task<bool> ToggleShuffleAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback == null)
                {
                    return false;
                }

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

        /// <summary>
        /// Cycles through repeat modes: Off -> Context -> Track -> Off.
        /// </summary>
        /// <returns>True if repeat mode was successfully changed; otherwise, false.</returns>
        public async Task<bool> ToggleRepeatAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback == null)
                {
                    return false;
                }

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

        /// <summary>
        /// Saves the currently playing track to the user's Spotify library.
        /// </summary>
        /// <returns>True if the track was successfully saved; otherwise, false.</returns>
        public async Task<bool> SaveCurrentTrackAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback?.CurrentTrack == null)
                {
                    return false;
                }

                var uri = Uri.EscapeDataString(playback.CurrentTrack.Uri);
                var url = $"https://api.spotify.com/v1/me/library?uris={uri}";

                return await SendPlayerCommandAsync(url, HttpMethod.Put, body: null, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving track");
                return false;
            }
        }

        /// <summary>
        /// Removes the currently playing track from the user's Spotify library.
        /// </summary>
        /// <returns>True if the track was successfully removed; otherwise, false.</returns>
        public async Task<bool> RemoveCurrentTrackAsync()
        {
            try
            {
                var playback = await GetCurrentPlaybackAsync();
                if (playback?.CurrentTrack == null)
                {
                    return false;
                }

                var uri = Uri.EscapeDataString(playback.CurrentTrack.Uri);
                var url = $"https://api.spotify.com/v1/me/library?uris={uri}";

                return await SendPlayerCommandAsync(url, HttpMethod.Delete, body: null, useFullUrl: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing track");
                return false;
            }
        }

        /// <summary>
        /// Seeks to a specific position in the currently playing track.
        /// Position is automatically clamped to a minimum of 0.
        /// </summary>
        /// <param name="positionMs">The position in milliseconds.</param>
        /// <returns>True if the seek operation was successful; otherwise, false.</returns>
        public async Task<bool> SeekToPositionAsync(int positionMs)
        {
            positionMs = Math.Max(0, positionMs);
            var url = $"https://api.spotify.com/v1/me/player/seek?position_ms={positionMs}";
            return await SendPlayerCommandAsync(url, HttpMethod.Put, useFullUrl: true);
        }

        /// <summary>
        /// Ensures that a valid authentication token is available for API requests.
        /// Refreshes the token or reloads stored tokens as needed.
        /// </summary>
        /// <returns>True if a valid token is available; otherwise, false.</returns>
        private async Task<bool> EnsureValidTokenAsync()
        {
            if (_currentAuth == null)
            {
                // Try to load stored authentication first before giving up
                LoadStoredAuthentication();

                if (_currentAuth == null)
                {
                    _logger.LogWarning("No authentication available. Please authenticate through the UI.");
                    return false;
                }
            }

            if (_authenticationService.IsTokenValid(_currentAuth))
            {
                return true;
            }

            if (await RefreshTokenCoreAsync(force: false))
            {
                return true;
            }

            // The refresh token may have been revoked — for example the user
            // re-authenticated through the UI, which stored new tokens this session
            // has not seen yet. Fall back to the stored tokens before giving up.
            LoadStoredAuthentication();
            return _currentAuth != null && _authenticationService.IsTokenValid(_currentAuth);
        }

        /// <summary>
        /// Sends an authorized request to the Spotify API, refreshing the token and
        /// retrying once if the token is rejected before its expected expiry.
        /// </summary>
        /// <param name="createRequest">Factory that builds the request; called again for the retry.</param>
        /// <returns>The HTTP response, or null if no valid token is available.</returns>
        private async Task<HttpResponseMessage?> SendRequestAsync(Func<HttpRequestMessage> createRequest)
        {
            if (!await EnsureValidTokenAsync())
            {
                return null;
            }

            var response = await SendWithBearerTokenAsync(createRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // The token was rejected before its expected expiry (revoked or clock
                // skew) — refresh and retry once
                _logger.LogInformation("Received 401 from Spotify API, refreshing token and retrying once");
                if (await RefreshTokenCoreAsync(force: true))
                {
                    response.Dispose();
                    response = await SendWithBearerTokenAsync(createRequest);
                }
            }

            return response;
        }

        /// <summary>
        /// Builds a request via the factory, attaches the current bearer token, and sends it.
        /// </summary>
        private async Task<HttpResponseMessage> SendWithBearerTokenAsync(Func<HttpRequestMessage> createRequest)
        {
            var request = createRequest();
            _logger.LogDebug("{Method} {Url}", request.Method.Method, request.RequestUri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentAuth!.AccessToken);
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Sends a command to the Spotify player API endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint or full URL.</param>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="body">Optional request body as JSON string.</param>
        /// <param name="useFullUrl">Whether the endpoint parameter is a full URL.</param>
        /// <returns>True if the command was successful; otherwise, false.</returns>
        private async Task<bool> SendPlayerCommandAsync(string endpoint, HttpMethod method, string? body = null, bool useFullUrl = false)
        {
            try
            {
                var url = useFullUrl ? endpoint : $"https://api.spotify.com/v1/me/player/{endpoint}";
                var response = await SendRequestAsync(() =>
                {
                    var request = new HttpRequestMessage(method, url);
                    if (!string.IsNullOrEmpty(body))
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }
                    return request;
                });

                return response != null && response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending player command: {Endpoint}", endpoint);
                return false;
            }
        }

        /// <summary>
        /// Parses the JSON response from Spotify API into a PlaybackState object.
        /// Several fields ("item", "progress_ms", device "volume_percent") are
        /// documented as nullable — e.g. during ad breaks or private sessions —
        /// so they all fall back to defaults instead of throwing.
        /// </summary>
        /// <param name="root">The root JSON element from the API response.</param>
        /// <returns>A populated PlaybackState object.</returns>
        private static PlaybackState ParsePlaybackState(JsonElement root)
        {
            var playbackState = new PlaybackState
            {
                IsPlaying = root.GetProperty("is_playing").GetBoolean(),
                ShuffleState = root.GetProperty("shuffle_state").GetBoolean(),
                ProgressMs = GetInt32OrDefault(root, "progress_ms")
            };

            // Parse repeat state
            playbackState.RepeatState = GetStringOrEmpty(root, "repeat_state") switch
            {
                "off" => RepeatMode.Off,
                "track" => RepeatMode.Track,
                "context" => RepeatMode.Context,
                _ => RepeatMode.Off
            };

            // Parse current track ("item" is null during ad breaks and for episodes
            // when additional_types is not requested)
            if (root.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.Object)
            {
                var durationMs = GetInt32OrDefault(item, "duration_ms");
                playbackState.DurationMs = durationMs;

                playbackState.CurrentTrack = new Track
                {
                    Id = GetStringOrEmpty(item, "id"),
                    Name = GetStringOrEmpty(item, "name"),
                    Uri = GetStringOrEmpty(item, "uri"),
                    DurationMs = durationMs,
                    Artist = item.TryGetProperty("artists", out var artists) &&
                             artists.ValueKind == JsonValueKind.Array &&
                             artists.GetArrayLength() > 0
                        ? GetStringOrEmpty(artists[0], "name")
                        : string.Empty,
                    Album = item.TryGetProperty("album", out var album) && album.ValueKind == JsonValueKind.Object
                        ? GetStringOrEmpty(album, "name")
                        : string.Empty,
                    IsPlaying = playbackState.IsPlaying,
                    ProgressMs = playbackState.ProgressMs
                };
            }

            // Parse device
            if (root.TryGetProperty("device", out var device) && device.ValueKind == JsonValueKind.Object)
            {
                playbackState.Volume = GetInt32OrDefault(device, "volume_percent");
                playbackState.Device = ParseDevice(device);
            }

            return playbackState;
        }

        /// <summary>
        /// Parses a device JSON element into a Device object.
        /// </summary>
        /// <param name="device">The device JSON element.</param>
        /// <returns>A populated Device object.</returns>
        private static Device ParseDevice(JsonElement device)
        {
            return new Device
            {
                Id = GetStringOrEmpty(device, "id"),
                Name = GetStringOrEmpty(device, "name"),
                Type = GetStringOrEmpty(device, "type"),
                IsActive = device.GetProperty("is_active").GetBoolean(),
                IsPrivateSession = device.GetProperty("is_private_session").GetBoolean(),
                IsRestricted = device.GetProperty("is_restricted").GetBoolean(),
                VolumePercent = GetInt32OrDefault(device, "volume_percent")
            };
        }

        /// <summary>
        /// Reads an integer property that may be missing or JSON null, returning 0 in those cases.
        /// </summary>
        private static int GetInt32OrDefault(JsonElement parent, string propertyName)
        {
            return parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : 0;
        }

        /// <summary>
        /// Reads a string property that may be missing or JSON null, returning an empty string in those cases.
        /// </summary>
        private static string GetStringOrEmpty(JsonElement parent, string propertyName)
        {
            return parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
