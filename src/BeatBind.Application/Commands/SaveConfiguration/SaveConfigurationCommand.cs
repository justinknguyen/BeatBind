using BeatBind.Application.Abstractions;
using BeatBind.Core.Entities;

namespace BeatBind.Application.Commands.SaveConfiguration
{
    public sealed record SaveConfigurationCommand(ApplicationConfiguration Configuration) : ICommand;
}
