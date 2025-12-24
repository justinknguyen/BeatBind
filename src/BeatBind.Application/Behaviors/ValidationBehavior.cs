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

            var validationFailures = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            var errors = validationFailures
                .Where(validationResult => !validationResult.IsValid)
                .SelectMany(validationResult => validationResult.Errors)
                .Select(validationFailure => validationFailure.ErrorMessage)
                .ToList();

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);

                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var genericArgument = typeof(TResponse).GetGenericArguments()[0];
                    var genericMethod = typeof(Result).GetMethods()
                        .First(m => m.Name == "Failure" && m.IsGenericMethod)
                        .MakeGenericMethod(genericArgument);
                    return (TResponse)genericMethod.Invoke(null, new object[] { errorMessage })!;
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(errorMessage);
                }
                
                throw new ValidationException(string.Join("; ", errors));
            }

            return await next();
        }
    }
}
