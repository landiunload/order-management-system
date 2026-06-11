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

    private static async Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string problemTitle,
        string problemDetail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = problemTitle,
            Detail = problemDetail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails);
    }
}
