using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;

namespace BeatBind.Application.Configuration.Commands.SaveConfiguration
{
    public sealed class SaveConfigurationCommandHandler : IRequestHandler<SaveConfigurationCommand, Result>
    {
        private readonly IConfigurationService _configurationService;

        public SaveConfigurationCommandHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public Task<Result> Handle(SaveConfigurationCommand request, CancellationToken cancellationToken)
        {
            _configurationService.SaveConfiguration(request.Configuration);
            return Task.FromResult(Result.Success());
        }
    }
}
