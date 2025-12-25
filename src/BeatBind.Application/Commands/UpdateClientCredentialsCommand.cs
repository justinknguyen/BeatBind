using BeatBind.Application.Abstractions;

namespace BeatBind.Application.Commands
{
    public sealed record UpdateClientCredentialsCommand(string ClientId, string ClientSecret) : ICommand;
}
