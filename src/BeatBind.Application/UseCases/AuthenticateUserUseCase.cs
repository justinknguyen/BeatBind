using BeatBind.Domain.Entities;
using BeatBind.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.UseCases
{
    public class AuthenticateUserUseCase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<AuthenticateUserUseCase> _logger;

        public AuthenticateUserUseCase(
            ISpotifyService spotifyService,
            IConfigurationService configurationService,
            ILogger<AuthenticateUserUseCase> logger)
        {
            _spotifyService = spotifyService;
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    _logger.LogWarning("Client credentials are not configured");
                    return false;
                }

                _logger.LogInformation("Starting Spotify authentication...");
                var result = await _spotifyService.AuthenticateAsync();
                
                if (result)
                {
                    _logger.LogInformation("Authentication successful");
                }
                else
                {
                    _logger.LogError("Authentication failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication process failed");
                return false;
            }
        }
    }
}
