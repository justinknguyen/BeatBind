using BeatBind.Domain.Common;
using BeatBind.Domain.Interfaces;
using MediatR;

namespace BeatBind.Application.Configuration.Commands.UpdateClientCredentials
{
    public sealed class UpdateClientCredentialsCommandHandler : IRequestHandler<UpdateClientCredentialsCommand, Result>
    {
        private readonly IConfigurationService _configurationService;

        public UpdateClientCredentialsCommandHandler(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public Task<Result> Handle(UpdateClientCredentialsCommand request, CancellationToken cancellationToken)
        {
            _configurationService.UpdateClientCredentials(request.ClientId, request.ClientSecret);
            return Task.FromResult(Result.Success());
        }
    }
}
