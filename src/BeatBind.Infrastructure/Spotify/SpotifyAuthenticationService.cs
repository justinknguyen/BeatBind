using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Spotify
{
    public class SpotifyAuthenticationService : IAuthenticationService
    {
        private readonly ILogger<SpotifyAuthenticationService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly HttpClient _httpClient;
        private HttpListener? _httpListener;

        public SpotifyAuthenticationService(
            ILogger<SpotifyAuthenticationService> logger,
            IConfigurationService configurationService,
            HttpClient httpClient)
        {
            _logger = logger;
            _configurationService = configurationService;
            _httpClient = httpClient;
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    return new AuthenticationResult { Success = false, Error = "Client ID and Client Secret are required" };
                }

                // Generate state for security
                var state = Guid.NewGuid().ToString("N");
                
                // Start local HTTP listener for callback
                if (!await StartCallbackListenerAsync(config.RedirectUri))
                {
                    return new AuthenticationResult { Success = false, Error = "Failed to start callback listener" };
                }

                // Build authorization URL
                var authUrl = BuildAuthorizationUrl(config.ClientId, config.RedirectUri, state);
                
                // Open browser for user authentication
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

                // Wait for callback
                var callbackResult = await WaitForCallbackAsync(state);
                
                if (!callbackResult.Success)
                {
                    return callbackResult;
                }

                // Exchange authorization code for access token
                return await ExchangeCodeForTokenAsync(callbackResult.AccessToken, config); // AccessToken contains the code here
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
            finally
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                var parameters = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                };

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
                
                var url = "https://accounts.spotify.com/api/token";
                _logger.LogInformation("POST {Url} (refresh_token)", url);
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                request.Content = new FormUrlEncodedContent(parameters);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    return new AuthenticationResult
                    {
                        Success = true,
                        AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
                        RefreshToken = root.TryGetProperty("refresh_token", out var refreshProp) ? 
                            refreshProp.GetString() ?? refreshToken : refreshToken,
                        ExpiresIn = root.GetProperty("expires_in").GetInt32(),
                        ExpiresAt = DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32())
                    };
                }
                else
                {
                    _logger.LogError("Token refresh failed: {Content}", content);
                    return new AuthenticationResult { Success = false, Error = "Token refresh failed" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }

        public bool IsTokenValid(AuthenticationResult authResult)
        {
            return authResult != null && 
                   !string.IsNullOrEmpty(authResult.AccessToken) && 
                   DateTime.UtcNow < authResult.ExpiresAt;
        }

        public AuthenticationResult? GetStoredAuthentication()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                if (string.IsNullOrEmpty(config.AccessToken) || string.IsNullOrEmpty(config.RefreshToken))
                {
                    return null;
                }

                return new AuthenticationResult
                {
                    Success = true,
                    AccessToken = config.AccessToken,
                    RefreshToken = config.RefreshToken,
                    ExpiresAt = config.TokenExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stored authentication");
                return null;
            }
        }

        public void SaveAuthentication(AuthenticationResult authResult)
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                config.AccessToken = authResult.AccessToken;
                config.RefreshToken = authResult.RefreshToken;
                config.TokenExpiresAt = authResult.ExpiresAt;
                
                // Save the updated configuration
                _configurationService.SaveConfiguration(config);
                
                _logger.LogInformation("Authentication tokens saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving authentication tokens");
            }
        }

        private Task<bool> StartCallbackListenerAsync(string redirectUri)
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(redirectUri.EndsWith('/') ? redirectUri : redirectUri + "/");
                _httpListener.Start();
                _logger.LogInformation("Started HTTP listener on {RedirectUri}", redirectUri);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start HTTP listener");
                return Task.FromResult(false);
            }
        }

        private static string BuildAuthorizationUrl(string clientId, string redirectUri, string state)
        {
            var scopes = "user-read-playback-state,user-modify-playback-state,user-read-currently-playing,user-library-read,user-library-modify";
            
            var parameters = new NameValueCollection
            {
                ["client_id"] = clientId,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri,
                ["state"] = state,
                ["scope"] = scopes,
                ["show_dialog"] = "true"
            };

            var query = string.Join("&", parameters.AllKeys.Select(key => $"{key}={HttpUtility.UrlEncode(parameters[key])}"));
            return $"https://accounts.spotify.com/authorize?{query}";
        }

        private async Task<AuthenticationResult> WaitForCallbackAsync(string expectedState)
        {
            try
            {
                var context = await _httpListener!.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Send response to browser
                var responseString = "<html><head><title>BeatBind</title></head><body><h1>Authentication Complete</h1><p>You can close this window.</p><script>window.close();</script></body></html>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // Parse callback parameters
                var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);
                var code = queryParams["code"];
                var state = queryParams["state"];
                var error = queryParams["error"];

                if (!string.IsNullOrEmpty(error))
                {
                    return new AuthenticationResult { Success = false, Error = $"Authorization error: {error}" };
                }

                if (state != expectedState)
                {
                    return new AuthenticationResult { Success = false, Error = "Invalid state parameter" };
                }

                if (string.IsNullOrEmpty(code))
                {
                    return new AuthenticationResult { Success = false, Error = "No authorization code received" };
                }

                return new AuthenticationResult { Success = true, AccessToken = code }; // Temporarily store code in AccessToken
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for callback");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<AuthenticationResult> ExchangeCodeForTokenAsync(string code, ApplicationConfiguration config)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = config.RedirectUri
                };

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
                
                var url = "https://accounts.spotify.com/api/token";
                _logger.LogInformation("POST {Url} (authorization_code)", url);
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                request.Content = new FormUrlEncodedContent(parameters);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    return new AuthenticationResult
                    {
                        Success = true,
                        AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
                        RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
                        ExpiresIn = root.GetProperty("expires_in").GetInt32(),
                        ExpiresAt = DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32())
                    };
                }
                else
                {
                    _logger.LogError("Token exchange failed: {Content}", content);
                    return new AuthenticationResult { Success = false, Error = "Token exchange failed" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for token");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }
    }
}
