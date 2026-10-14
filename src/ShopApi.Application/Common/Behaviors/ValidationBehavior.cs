using FluentValidation;
using MediatR;
using ShopApi.Application.Common.Wrappers;

namespace ShopApi.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        // C# cannot express `where TResponse : Result<TValue>`, so the typed
        // failure is constructed through the generic argument of TResponse.
        // This is the single sanctioned reflection spot in the codebase.
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failure = typeof(Result)
            .GetMethod(nameof(Result.Failure))!
            .MakeGenericMethod(valueType)
            .Invoke(null, new object[] { Error.Validation(failures[0].ErrorMessage) });

        return (TResponse)failure!;
    }
}
