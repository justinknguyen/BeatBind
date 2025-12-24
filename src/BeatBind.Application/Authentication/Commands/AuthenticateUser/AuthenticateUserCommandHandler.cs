using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Authentication.Commands.AuthenticateUser
{
    public sealed class AuthenticateUserCommandHandler : IRequestHandler<AuthenticateUserCommand, Result>
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<AuthenticateUserCommandHandler> _logger;

        public AuthenticateUserCommandHandler(
            ISpotifyService spotifyService,
            IConfigurationService configurationService,
            ILogger<AuthenticateUserCommandHandler> logger)
        {
            _spotifyService = spotifyService;
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<Result> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var config = _configurationService.GetConfiguration();
                
                if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
                {
                    _logger.LogWarning("Client credentials are not configured");
                    return Result.Failure("Client credentials are not configured");
                }

                _logger.LogInformation("Starting Spotify authentication...");
                var result = await _spotifyService.AuthenticateAsync();
                
                if (result)
                {
                    _logger.LogInformation("Authentication successful");
                    return Result.Success();
                }
                else
                {
                    _logger.LogError("Authentication failed");
                    return Result.Failure("Authentication failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return Result.Failure(ex.Message);
            }
        }
    }
}
