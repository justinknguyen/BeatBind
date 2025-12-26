using BeatBind.Core.Common;
using BeatBind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Services
{
    public class AuthenticationApplicationService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<AuthenticationApplicationService> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationApplicationService class.
        /// </summary>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="logger">The logger instance.</param>
        public AuthenticationApplicationService(
            IAuthenticationService authenticationService,
            IConfigurationService configurationService,
            ILogger<AuthenticationApplicationService> logger)
        {
            _authenticationService = authenticationService;
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates the user with Spotify and verifies that client credentials are configured.
        /// </summary>
        /// <returns>A Result indicating success or failure with error details.</returns>
        public async Task<Result> AuthenticateUserAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();

                if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    _logger.LogWarning("Client credentials are not configured");
                    return Result.Failure("Client credentials are not configured");
                }

                var authResult = await _authenticationService.AuthenticateAsync();

                if (authResult.Success)
                {
                    _logger.LogInformation("User authenticated successfully");
                    return Result.Success();
                }

                _logger.LogWarning("Authentication failed: {Error}", authResult.Error);
                return Result.Failure(authResult.Error ?? "Authentication failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user authentication");
                return Result.Failure("An error occurred during authentication");
            }
        }

        /// <summary>
        /// Updates the Spotify client credentials after validation.
        /// </summary>
        /// <param name="clientId">The Spotify application client ID.</param>
        /// <param name="clientSecret">The Spotify application client secret.</param>
        /// <returns>A Result indicating success or failure with error details.</returns>
        public async Task<Result> UpdateClientCredentialsAsync(string clientId, string clientSecret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    return Result.Failure("Client ID and Client Secret are required");
                }

                var config = _configurationService.GetConfiguration();
                config.ClientId = clientId;
                config.ClientSecret = clientSecret;
                _configurationService.SaveConfiguration(config);

                _logger.LogInformation("Client credentials updated successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client credentials");
                return Result.Failure("An error occurred while updating credentials");
            }
        }
    }
}
