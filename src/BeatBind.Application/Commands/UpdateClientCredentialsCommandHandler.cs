using BeatBind.Core.Common;
using BeatBind.Core.Interfaces;
using MediatR;

namespace BeatBind.Application.Commands
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
