using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;

namespace BeatBind.Application.Authentication.Commands.AuthenticateUser
{
    public sealed class AuthenticateUserCommandHandler : IRequestHandler<AuthenticateUserCommand, Result>
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IConfigurationService _configurationService;

        public AuthenticateUserCommandHandler(
            ISpotifyService spotifyService,
            IConfigurationService configurationService)
        {
            _spotifyService = spotifyService;
            _configurationService = configurationService;
        }

        public async Task<Result> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var config = _configurationService.GetConfiguration();
            
            if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
            {
                return Result.Failure("Client credentials are not configured");
            }

            var success = await _spotifyService.AuthenticateAsync();
            
            return success 
                ? Result.Success() 
                : Result.Failure("Authentication failed");
        }
    }
}
