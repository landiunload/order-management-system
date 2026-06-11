using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderManagement.Application.Common.Behaviors;

/// <summary>
/// Конвейерный behavior MediatR: структурированно логирует начало, завершение
/// и длительность обработки каждого запроса приложения.
/// </summary>
public sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> nextHandlerInPipeline,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Начата обработка запроса {ИмяЗапроса}", requestName);

        var executionStopwatch = Stopwatch.StartNew();
        try
        {
            var response = await nextHandlerInPipeline();
            executionStopwatch.Stop();

            logger.LogInformation(
                "Запрос {ИмяЗапроса} обработан успешно за {ДлительностьМиллисекунд} мс",
                requestName,
                executionStopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception processingException)
        {
            executionStopwatch.Stop();

            logger.LogError(
                processingException,
                "Запрос {ИмяЗапроса} завершился ошибкой через {ДлительностьМиллисекунд} мс",
                requestName,
                executionStopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
