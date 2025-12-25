using BeatBind.Application.Abstractions.Messaging;
using BeatBind.Core.Entities;

namespace BeatBind.Application.Commands.SaveConfiguration
{
    public sealed record SaveConfigurationCommand(ApplicationConfiguration Configuration) : ICommand;
}
