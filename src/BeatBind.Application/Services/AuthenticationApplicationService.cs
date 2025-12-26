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

        public AuthenticationApplicationService(
            IAuthenticationService authenticationService,
            IConfigurationService configurationService,
            ILogger<AuthenticationApplicationService> logger)
        {
            _authenticationService = authenticationService;
            _configurationService = configurationService;
            _logger = logger;
        }

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
    }
}
