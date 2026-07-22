using FluentValidation;
using Mediator;

namespace OrderManagement.Application.Common.Behaviors;

/// <summary>
/// Конвейерный behavior: автоматически прогоняет каждый запрос
/// через все зарегистрированные валидаторы FluentValidation до вызова обработчика.
/// Благодаря этому обработчики занимаются только бизнес-логикой (принцип единственной ответственности).
/// </summary>
public sealed class RequestValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> requestValidators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> nextHandlerInPipeline,
        CancellationToken cancellationToken)
    {
        if (!requestValidators.Any())
        {
            return await nextHandlerInPipeline(request, cancellationToken);
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

        return await nextHandlerInPipeline(request, cancellationToken);
    }
}
