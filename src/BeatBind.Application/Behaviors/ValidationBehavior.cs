using BeatBind.Domain.Common;
using FluentValidation;
using MediatR;

namespace BeatBind.Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
        where TResponse : Result
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            var errors = validationResults
                .Where(result => !result.IsValid)
                .SelectMany(result => result.Errors)
                .Select(failure => failure.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                return CreateFailureResult(string.Join("; ", errors));
            }

            return await next();
        }

        private static TResponse CreateFailureResult(string errorMessage)
        {
            // Use reflection to call the appropriate Failure method
            var resultType = typeof(TResponse);
            
            if (resultType.IsGenericType)
            {
                var genericArgument = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .First(m => m.Name == "Failure" && m.IsGenericMethod)
                    .MakeGenericMethod(genericArgument);
                return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage })!;
            }
            
            return (TResponse)(object)Result.Failure(errorMessage);
        }
    }
}
