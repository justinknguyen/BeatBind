using BeatBind.Application.Abstractions;

namespace BeatBind.Application.Commands.UpdateClientCredentials
{
    public sealed record UpdateClientCredentialsCommand(string ClientId, string ClientSecret) : ICommand;
}
