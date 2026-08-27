using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace OrderManagement.Application.Common.Behaviors;

/// <summary>
/// Конвейерный behavior: структурированно логирует начало, завершение
/// и длительность обработки каждого запроса приложения.
/// </summary>
public sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private static readonly Action<ILogger, string, double, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(1001, "ApplicationRequestCompleted"),
            "Запрос {ИмяЗапроса} обработан успешно за {ДлительностьМиллисекунд} мс");

    private static readonly Action<ILogger, string, double, Exception?> LogRequestFailed =
        LoggerMessage.Define<string, double>(
            LogLevel.Debug,
            new EventId(1002, "ApplicationRequestFailed"),
            "Запрос {ИмяЗапроса} завершился ошибкой через {ДлительностьМиллисекунд} мс");

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var response = await next(message, cancellationToken);
            LogRequestCompleted(
                logger,
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                null);

            return response;
        }
        catch
        {
            // Ошибку с контекстом HTTP пишет единый middleware; здесь оставляем только
            // дешёвый диагностический тайминг и не дублируем stack trace в production-логах.
            LogRequestFailed(
                logger,
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                null);

            throw;
        }
    }
}
