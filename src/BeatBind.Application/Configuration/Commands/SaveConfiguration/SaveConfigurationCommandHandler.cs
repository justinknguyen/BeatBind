using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Configuration.Commands.SaveConfiguration
{
    public sealed class SaveConfigurationCommandHandler : IRequestHandler<SaveConfigurationCommand, Result>
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SaveConfigurationCommandHandler> _logger;

        public SaveConfigurationCommandHandler(
            IConfigurationService configurationService,
            ILogger<SaveConfigurationCommandHandler> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public Task<Result> Handle(SaveConfigurationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _configurationService.SaveConfiguration(request.Configuration);
                _logger.LogInformation("Configuration saved successfully");
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration");
                return Task.FromResult(Result.Failure(ex.Message));
            }
        }
    }
}
