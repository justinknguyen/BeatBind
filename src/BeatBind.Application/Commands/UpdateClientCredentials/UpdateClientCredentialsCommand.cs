using BeatBind.Application.Abstractions.Messaging;

namespace BeatBind.Application.Commands.UpdateClientCredentials
{
    public sealed record UpdateClientCredentialsCommand(string ClientId, string ClientSecret) : ICommand;
}
