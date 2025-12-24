using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Domain.Entities;

namespace BeatBind.Application.Configuration.Commands.SaveConfiguration
{
    public sealed record SaveConfigurationCommand(ApplicationConfiguration Configuration) : ICommand;
}
