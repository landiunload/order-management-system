using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Exceptions;
using OrderManagement.WebApi.Middleware;
using Xunit;

namespace OrderManagement.UnitTests.WebApi;

/// <summary>
/// Тесты глобального обработчика исключений. Это единственное место, где тип
/// исключения нижних слоёв превращается в конкретный HTTP-код, и до сих пор
/// сопоставление не проверял ни один тест: перепутать местами 404 и 409 или
/// раскрыть текст внутренней ошибки наружу можно было незаметно. Каждый случай
/// прогоняется через настоящий <see cref="DefaultHttpContext"/>, а тело ответа
/// разбирается обратно в <see cref="ProblemDetails"/>.
/// </summary>
public sealed class GlobalExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    // Прогоняет middleware с next, который бросает заданное исключение, и
    // возвращает итоговый статус вместе с разобранным телом ProblemDetails.
    private static async Task<(int StatusCode, ProblemDetails Problem)> InvokeWithThrownAsync(Exception thrown)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/orders";
        httpContext.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw thrown,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            WebJsonOptions);

        Assert.NotNull(problem);
        return (httpContext.Response.StatusCode, problem);
    }

    [Fact]
    public async Task InvokeAsync_ОшибкаВалидации_Возвращает400СТекстомОшибки()
    {
        var thrown = new ValidationException([new ValidationFailure("Поле", "Сообщение об ошибке")]);

        var (statusCode, problem) = await InvokeWithThrownAsync(thrown);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("Сообщение об ошибке", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_СущностьНеНайдена_Возвращает404()
    {
        var thrown = new EntityNotFoundException("Заказ", Guid.CreateVersion7());

        var (statusCode, problem) = await InvokeWithThrownAsync(thrown);

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task InvokeAsync_НарушениеБизнесПравила_Возвращает409СОписанием()
    {
        var thrown = new DomainRuleViolationException("Заказ уже подтверждён");

        var (statusCode, problem) = await InvokeWithThrownAsync(thrown);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Contains("Заказ уже подтверждён", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_КонфликтПараллельногоОбновления_Возвращает409БезДеталейБазы()
    {
        var thrown = new DbUpdateConcurrencyException("внутренняя деталь провайдера БД");

        var (statusCode, problem) = await InvokeWithThrownAsync(thrown);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.DoesNotContain("провайдера БД", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_НепредвиденноеИсключение_Возвращает500БезУтечкиДеталей()
    {
        var thrown = new InvalidOperationException("секретная деталь из стектрейса");

        var (statusCode, problem) = await InvokeWithThrownAsync(thrown);

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        // Наружу уходит обобщённая формулировка, а не текст внутреннего исключения
        Assert.DoesNotContain("секретная деталь", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_ЛюбаяОшибка_ОтдаётТипСодержимогоRfc9457()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/orders";
        httpContext.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new DomainRuleViolationException("Заказ уже подтверждён"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        // README обещает ProblemDetails по RFC 9457, а стандарт опознаётся клиентом
        // именно по типу содержимого: с обычным application/json описание ошибки
        // неотличимо от нормального тела ответа.
        Assert.StartsWith(
            GlobalExceptionHandlingMiddleware.ProblemDetailsContentType,
            httpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ОтветУжеНачат_НеБросаетИНеТрогаетОтвет()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/orders";
        var responseBody = new MemoryStream();
        httpContext.Features.Set<IHttpResponseFeature>(new StartedResponseFeature(responseBody));
        httpContext.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBody));

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("сбой при сериализации длинного списка"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        // Без охранного условия middleware попытался бы выставить статус уже
        // отправленного ответа и получил бы второе исключение — оно вылетело бы
        // наружу и скрыло исходную ошибку.
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public async Task InvokeAsync_КлиентОтменилЗапрос_НеФормируетОшибку()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestAborted = new CancellationToken(canceled: true);
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => Task.FromCanceled(httpContext.RequestAborted),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    // Повторяет контракт настоящего сервера: Kestrel считает ответ начатым после
    // отправки заголовков и запрещает менять статус — именно это поведение и
    // должно быть учтено обработчиком.
    private sealed class StartedResponseFeature(Stream body) : IHttpResponseFeature
    {
        private int _statusCode = StatusCodes.Status200OK;

        public Stream Body { get; set; } = body;

        public bool HasStarted => true;

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public string? ReasonPhrase { get; set; }

        public int StatusCode
        {
            get => _statusCode;
            set => throw new InvalidOperationException(
                "StatusCode cannot be set because the response has already started.");
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }

    [Fact]
    public async Task InvokeAsync_ИсключенийНет_ПропускаетЗапросДальшеБезПодмены()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var nextWasCalled = false;

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }
}
