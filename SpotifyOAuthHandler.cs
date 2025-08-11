using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace BeatBind
{
    public class SpotifyOAuthHandler
    {
        private readonly ILogger<SpotifyOAuthHandler> _logger;
        private readonly ConfigurationManager _configManager;
        private readonly string _redirectUri;
        private HttpListener? _httpListener;

        public SpotifyOAuthHandler(ConfigurationManager configManager)
        {
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SpotifyOAuthHandler>();
            _configManager = configManager;
            _redirectUri = configManager.RedirectUri;
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_configManager.ClientId) || string.IsNullOrEmpty(_configManager.ClientSecret))
                {
                    return new AuthenticationResult { Success = false, Error = "Client ID and Client Secret are required" };
                }

                // Generate state for security
                var state = Guid.NewGuid().ToString("N");
                
                // Start local HTTP listener for callback
                if (!await StartCallbackListenerAsync())
                {
                    return new AuthenticationResult { Success = false, Error = "Failed to start callback listener" };
                }

                // Build authorization URL
                var authUrl = BuildAuthorizationUrl(state);
                
                // Open browser for user authentication
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                // Wait for callback
                var callbackResult = await WaitForCallbackAsync(state);
                
                if (!callbackResult.Success)
                {
                    return callbackResult;
                }

                // Exchange authorization code for access token
                var tokenResult = await ExchangeCodeForTokenAsync(callbackResult.AuthorizationCode!);
                
                return tokenResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth authentication");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
            finally
            {
                StopCallbackListener();
            }
        }

        private string BuildAuthorizationUrl(string state)
        {
            var scopes = new[]
            {
                "user-read-playback-state",
                "user-modify-playback-state",
                "user-read-currently-playing",
                "user-library-modify",
                "user-library-read"
            };

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["client_id"] = _configManager.ClientId;
            queryParams["response_type"] = "code";
            queryParams["redirect_uri"] = _redirectUri;
            queryParams["scope"] = string.Join(" ", scopes);
            queryParams["state"] = state;

            return $"https://accounts.spotify.com/authorize?{queryParams}";
        }

        private Task<bool> StartCallbackListenerAsync()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(_redirectUri.EndsWith("/") ? _redirectUri : _redirectUri + "/");
                _httpListener.Start();
                
                _logger.LogInformation($"Started HTTP listener on {_redirectUri}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start HTTP listener");
                return Task.FromResult(false);
            }
        }

        private async Task<AuthenticationResult> WaitForCallbackAsync(string expectedState)
        {
            try
            {
                if (_httpListener == null)
                {
                    return new AuthenticationResult { Success = false, Error = "HTTP listener not started" };
                }

                // Set timeout for waiting
                using var timeoutTokenSource = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
                
                while (!timeoutTokenSource.Token.IsCancellationRequested)
                {
                    var contextTask = _httpListener.GetContextAsync();
                    var context = await contextTask;
                    
                    var request = context.Request;
                    var response = context.Response;

                    try
                    {
                        // Parse query parameters
                        var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
                        
                        var code = queryParams["code"];
                        var state = queryParams["state"];
                        var error = queryParams["error"];

                        // Send response to browser
                        string responseString;
                        if (!string.IsNullOrEmpty(error))
                        {
                            responseString = CreateHtmlResponse("Authentication Error", $"Error: {error}", false);
                            response.StatusCode = 400;
                        }
                        else if (string.IsNullOrEmpty(code) || state != expectedState)
                        {
                            responseString = CreateHtmlResponse("Authentication Error", "Invalid response from Spotify", false);
                            response.StatusCode = 400;
                        }
                        else
                        {
                            responseString = CreateHtmlResponse("Authentication Successful", "You can now close this window and return to BeatBind", true);
                            response.StatusCode = 200;
                        }

                        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/html";
                        
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.OutputStream.Close();

                        // Return result
                        if (!string.IsNullOrEmpty(error))
                        {
                            return new AuthenticationResult { Success = false, Error = error };
                        }
                        else if (!string.IsNullOrEmpty(code) && state == expectedState)
                        {
                            return new AuthenticationResult { Success = true, AuthorizationCode = code };
                        }
                        else
                        {
                            return new AuthenticationResult { Success = false, Error = "Invalid callback response" };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing callback request");
                        response.StatusCode = 500;
                        response.OutputStream.Close();
                    }
                }

                return new AuthenticationResult { Success = false, Error = "Authentication timeout" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for callback");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<AuthenticationResult> ExchangeCodeForTokenAsync(string authorizationCode)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                    new KeyValuePair<string, string>("client_id", _configManager.ClientId),
                    new KeyValuePair<string, string>("client_secret", _configManager.ClientSecret)
                });

                var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", tokenRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Token exchange failed: {responseContent}");
                    return new AuthenticationResult { Success = false, Error = "Failed to exchange authorization code for token" };
                }

                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent);
                
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.access_token))
                {
                    return new AuthenticationResult 
                    { 
                        Success = true, 
                        AccessToken = tokenResponse.access_token,
                        RefreshToken = tokenResponse.refresh_token,
                        ExpiresIn = tokenResponse.expires_in
                    };
                }

                return new AuthenticationResult { Success = false, Error = "Invalid token response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for token");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }

        private void StopCallbackListener()
        {
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
                _httpListener = null;
                _logger.LogInformation("Stopped HTTP listener");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping HTTP listener");
            }
        }

        private string CreateHtmlResponse(string title, string message, bool success)
        {
            var color = success ? "#4CAF50" : "#f44336";
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
        .container {{ max-width: 500px; margin: 0 auto; }}
        .message {{ background-color: {color}; color: white; padding: 20px; border-radius: 5px; }}
        h1 {{ margin-top: 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='message'>
            <h1>{title}</h1>
            <p>{message}</p>
        </div>
    </div>
</body>
</html>";
        }

        public void Dispose()
        {
            StopCallbackListener();
        }
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class SpotifyTokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public string scope { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string? refresh_token { get; set; }
    }
}
