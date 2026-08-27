using FluentValidation;
using FluentValidation.Results;
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
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure>? validationFailures = null;

        // Валидаторы обычно завершаются синхронно. Последовательный проход не создаёт
        // массив задач на каждый запрос и не запускает пользовательские правила параллельно.
        foreach (var requestValidator in requestValidators)
        {
            var validationResult = await requestValidator.ValidateAsync(
                new ValidationContext<TRequest>(message), cancellationToken);

            if (!validationResult.IsValid)
            {
                validationFailures ??= [];
                validationFailures.AddRange(validationResult.Errors);
            }
        }

        if (validationFailures is { Count: > 0 })
        {
            throw new ValidationException(validationFailures);
        }

        return await next(message, cancellationToken);
    }
}
