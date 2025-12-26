using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using BeatBind.Core.Entities;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly HttpClient _httpClient;
        private HttpListener? _httpListener;

        /// <summary>
        /// Initializes a new instance of the AuthenticationService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="httpClient">The HTTP client for API requests.</param>
        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            IConfigurationService configurationService,
            HttpClient httpClient)
        {
            _logger = logger;
            _configurationService = configurationService;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Authenticates the user with Spotify using OAuth 2.0 authorization code flow.
        /// Opens a browser for user authentication and starts a local HTTP listener for the callback.
        /// </summary>
        /// <returns>An AuthenticationResult containing tokens and status information.</returns>
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

        /// <summary>
        /// Refreshes an expired access token using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to use for obtaining a new access token.</param>
        /// <returns>An AuthenticationResult containing the new tokens and status information.</returns>
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

        /// <summary>
        /// Checks if the authentication result contains a valid, non-expired access token.
        /// </summary>
        /// <param name="authResult">The authentication result to validate.</param>
        /// <returns>True if the token is valid and not expired; otherwise, false.</returns>
        public bool IsTokenValid(AuthenticationResult authResult)
        {
            return authResult != null &&
                   !string.IsNullOrEmpty(authResult.AccessToken) &&
                   DateTime.UtcNow < authResult.ExpiresAt;
        }

        /// <summary>
        /// Retrieves stored authentication tokens from the configuration.
        /// </summary>
        /// <returns>An AuthenticationResult if tokens exist; otherwise, null.</returns>
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

        /// <summary>
        /// Saves authentication tokens to the configuration for persistent storage.
        /// </summary>
        /// <param name="authResult">The authentication result containing tokens to save.</param>
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

        /// <summary>
        /// Starts a local HTTP listener to receive the OAuth callback.
        /// </summary>
        /// <param name="redirectUri">The redirect URI to listen on.</param>
        /// <returns>True if the listener started successfully; otherwise, false.</returns>
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

        /// <summary>
        /// Builds the Spotify authorization URL with required parameters and scopes.
        /// </summary>
        /// <param name="clientId">The Spotify application client ID.</param>
        /// <param name="redirectUri">The callback redirect URI.</param>
        /// <param name="state">A unique state value for CSRF protection.</param>
        /// <returns>The complete authorization URL.</returns>
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

        /// <summary>
        /// Waits for and processes the OAuth callback from Spotify.
        /// Displays a success page to the user and validates the callback parameters.
        /// </summary>
        /// <param name="expectedState">The expected state value for validation.</param>
        /// <returns>An AuthenticationResult containing the authorization code or error information.</returns>
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

        /// <summary>
        /// Exchanges an authorization code for access and refresh tokens.
        /// </summary>
        /// <param name="code">The authorization code received from the callback.</param>
        /// <param name="config">The application configuration containing client credentials.</param>
        /// <returns>An AuthenticationResult containing the access and refresh tokens.</returns>
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
