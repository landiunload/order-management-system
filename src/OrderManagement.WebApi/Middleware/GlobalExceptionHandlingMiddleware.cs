using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.WebApi.Middleware;

/// <summary>
/// Глобальное промежуточное программное обеспечение обработки исключений.
/// Преобразует типизированные исключения нижних слоёв в HTTP-ответы формата ProblemDetails (RFC 9457),
/// чтобы у API был единый предсказуемый формат ошибок.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(
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
        catch (ValidationException validationException)
        {
            // Ошибки валидации входных данных — это 400 Bad Request
            logger.LogWarning("Запрос отклонён валидацией: {ОшибкиВалидации}",
                string.Join("; ", validationException.Errors.Select(failure => failure.ErrorMessage)));

            await WriteProblemDetailsAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Ошибка валидации входных данных",
                string.Join(" ", validationException.Errors.Select(failure => failure.ErrorMessage)));
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
        catch (Exception unexpectedException)
        {
            logger.LogError(unexpectedException, "Необработанное исключение при обработке запроса {ПутьЗапроса}",
                httpContext.Request.Path);

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
            logger.LogError(
                "Ответ на {ПутьЗапроса} уже начат, ProblemDetails со статусом {КодСостояния} отправить нельзя",
                httpContext.Request.Path,
                statusCode);
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = problemTitle,
            Detail = problemDetail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        // RFC 9457 требует именно application/problem+json: по этому типу клиент
        // отличает описание ошибки от обычного тела ответа.
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: ProblemDetailsContentType);
    }
}
