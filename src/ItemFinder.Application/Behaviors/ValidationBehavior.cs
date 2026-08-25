using FluentValidation;

using MediatR;

namespace ItemFinder.Application.Behaviors;

/// <summary>Runs every registered validator for a request before its handler; failures surface as a ValidationException.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        if (!validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(result => result.Errors).ToList();
        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next().ConfigureAwait(false);
    }
}