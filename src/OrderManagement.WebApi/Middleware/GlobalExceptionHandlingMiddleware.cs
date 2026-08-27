using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.WebApi.Middleware;

/// <summary>
/// Глобальное промежуточное программное обеспечение обработки исключений.
/// Преобразует типизированные исключения нижних слоёв в HTTP-ответы формата ProblemDetails (RFC 9457),
/// чтобы у API был единый предсказуемый формат ошибок.
/// </summary>
public sealed partial class GlobalExceptionHandlingMiddleware(
    RequestDelegate nextMiddlewareInPipeline,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    /// <summary>Тип содержимого для описания ошибки по RFC 9457.</summary>
    public const string ProblemDetailsContentType = "application/problem+json";

    /// <summary>Обрабатывает запрос, перехватывая любые исключения нижних слоёв.</summary>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await nextMiddlewareInPipeline(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // Клиент отключился или сработал request timeout. Ответ ему уже не нужен,
            // а попытка сериализовать 500 только потратит ресурсы и зашумит error-логи.
            LogRequestCanceled(logger, httpContext.Request.Path);
        }
        catch (ValidationException validationException)
        {
            // Ошибки валидации входных данных — это 400 Bad Request
            var validationMessage = string.Join(
                " ",
                validationException.Errors
                    .Select(validationFailure => validationFailure.ErrorMessage)
                    .Distinct(StringComparer.Ordinal));

            LogValidationRejected(logger, validationMessage);

            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Ошибка валидации входных данных",
                validationMessage);
        }
        catch (EntityNotFoundException entityNotFoundException)
        {
            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status404NotFound,
                "Сущность не найдена",
                entityNotFoundException.Message);
        }
        catch (DomainRuleViolationException domainRuleViolationException)
        {
            // Нарушение бизнес-правила — это конфликт состояния, 409 Conflict
            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Нарушение бизнес-правила",
                domainRuleViolationException.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Конфликт параллельного изменения",
                "Состояние заказа уже изменилось. Получите актуальную версию и повторите запрос.");
        }
        catch (Exception unexpectedException)
        {
            LogUnexpectedException(logger, httpContext.Request.Path, unexpectedException);

            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка. Попробуйте повторить запрос позже.");
        }
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string problemTitle,
        string problemDetail)
    {
        // Исключение могло возникнуть уже после того, как часть ответа ушла клиенту
        // (например, при сериализации длинного списка). Тогда заголовки отправлены,
        // и запись статуса бросит второе исключение — оно вылетит мимо middleware и
        // скроет исходную ошибку. Ответ уже не спасти: пишем в лог и не трогаем его.
        if (httpContext.Response.HasStarted)
        {
            LogResponseAlreadyStarted(logger, httpContext.Request.Path, statusCode);
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = problemTitle,
            Detail = problemDetail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;

        // RFC 9457 требует именно application/problem+json: по этому типу клиент
        // отличает описание ошибки от обычного тела ответа.
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: ProblemDetailsContentType,
            cancellationToken: httpContext.RequestAborted);
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Обработка запроса {RequestPath} отменена")]
    private static partial void LogRequestCanceled(ILogger logger, PathString requestPath);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Запрос отклонён валидацией: {ValidationMessage}")]
    private static partial void LogValidationRejected(ILogger logger, string validationMessage);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Error,
        Message = "Необработанное исключение при обработке запроса {RequestPath}")]
    private static partial void LogUnexpectedException(
        ILogger logger,
        PathString requestPath,
        Exception exception);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "Ответ на {RequestPath} уже начат, ProblemDetails со статусом {StatusCode} отправить нельзя")]
    private static partial void LogResponseAlreadyStarted(
        ILogger logger,
        PathString requestPath,
        int statusCode);
}
