using BeatBind.Core.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BeatBind.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
        where TResponse : Result
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Processing request {RequestName}", requestName);

            try
            {
                var response = await next();

                if (response.IsSuccess)
                {
                    _logger.LogInformation("Completed request {RequestName}", requestName);
                }
                else
                {
                    _logger.LogWarning("Request {RequestName} failed. Error: {Error}", requestName, response.Error);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request {RequestName} failed with exception", requestName);
                throw;
            }
        }
    }
}
