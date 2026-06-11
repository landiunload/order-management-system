using FluentValidation;
using MediatR;

namespace OrderManagement.Application.Common.Behaviors;

/// <summary>
/// Конвейерный behavior MediatR: автоматически прогоняет каждый запрос
/// через все зарегистрированные валидаторы FluentValidation до вызова обработчика.
/// Благодаря этому обработчики занимаются только бизнес-логикой (принцип единственной ответственности).
/// </summary>
public sealed class RequestValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> requestValidators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> nextHandlerInPipeline,
        CancellationToken cancellationToken)
    {
        if (!requestValidators.Any())
        {
            return await nextHandlerInPipeline();
        }

        var validationContext = new ValidationContext<TRequest>(request);

        var validationFailures = (await Task.WhenAll(
                requestValidators.Select(validator => validator.ValidateAsync(validationContext, cancellationToken))))
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure is not null)
            .ToList();

        if (validationFailures.Count != 0)
        {
            throw new ValidationException(validationFailures);
        }

        return await nextHandlerInPipeline();
    }
}
