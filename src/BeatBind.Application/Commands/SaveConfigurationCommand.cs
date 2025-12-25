using BeatBind.Application.Abstractions;
using BeatBind.Core.Entities;

namespace BeatBind.Application.Commands
{
    public sealed record SaveConfigurationCommand(ApplicationConfiguration Configuration) : ICommand;
}
