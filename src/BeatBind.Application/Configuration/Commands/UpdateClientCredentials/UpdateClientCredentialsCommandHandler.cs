using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Configuration.Commands.UpdateClientCredentials
{
    public sealed class UpdateClientCredentialsCommandHandler : IRequestHandler<UpdateClientCredentialsCommand, Result>
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<UpdateClientCredentialsCommandHandler> _logger;

        public UpdateClientCredentialsCommandHandler(
            IConfigurationService configurationService,
            ILogger<UpdateClientCredentialsCommandHandler> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public Task<Result> Handle(UpdateClientCredentialsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _configurationService.UpdateClientCredentials(request.ClientId, request.ClientSecret);
                _logger.LogInformation("Client credentials saved successfully");
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving client credentials");
                return Task.FromResult(Result.Failure(ex.Message));
            }
        }
    }
}
