using BeatBind.Core.Common;
using MediatR;

namespace BeatBind.Application.Abstractions
{
    public interface ICommand : IRequest<Result>
    {
    }

    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {
    }

    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
}
