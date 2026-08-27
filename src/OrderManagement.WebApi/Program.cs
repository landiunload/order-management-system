using Microsoft.AspNetCore.Http.Timeouts;
using OrderManagement.Application;
using OrderManagement.Infrastructure;
using OrderManagement.WebApi.Middleware;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

// Заказ с сотней позиций занимает намного меньше мегабайта. Жёсткий предел не даёт
// держать большие тела запросов в памяти и применяется до JSON-десериализации.
webApplicationBuilder.WebHost.ConfigureKestrel(kestrelOptions =>
    kestrelOptions.Limits.MaxRequestBodySize = 1_048_576);

// Каждый слой регистрирует свои зависимости самостоятельно
webApplicationBuilder.Services.AddApplicationLayer();
webApplicationBuilder.Services.AddInfrastructureLayer(webApplicationBuilder.Configuration);

// Сохраняем суффикс «Async» в именах действий, чтобы CreatedAtAction(nameof(...)) находил маршрут
webApplicationBuilder.Services
    .AddControllers(mvcOptions => mvcOptions.SuppressAsyncSuffixInActionNames = false)
    .AddJsonOptions(jsonOptions => jsonOptions.JsonSerializerOptions.MaxDepth = 16);
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddSwaggerGen();
webApplicationBuilder.Services.AddRequestTimeouts(timeoutOptions =>
    timeoutOptions.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    });

var webApplication = webApplicationBuilder.Build();

// В среде разработки создаём схему базы данных при старте;
// в производственной среде вместо этого применялись бы миграции EF Core
if (webApplication.Environment.IsDevelopment())
{
    await EnsureDatabaseCreatedWithRetriesAsync(webApplication);
}

// Глобальный перехват ошибок — единый формат ответов ProblemDetails для всех исключений
webApplication.UseMiddleware<GlobalExceptionHandlingMiddleware>();
webApplication.UseRequestTimeouts();

if (webApplication.Environment.IsDevelopment())
{
    webApplication.UseSwagger();
    webApplication.UseSwaggerUI();
}

webApplication.MapControllers();
webApplication.MapGet("/health/live", () => Results.NoContent())
    .ExcludeFromDescription();

webApplication.Run();

// База может быть ещё не готова: depends_on в docker compose стережёт только первый
// запуск, а контейнер приложения переживает перезапуски независимо от базы. Без
// повторов служба падала на старте и не поднималась, пока база не вернётся, — то есть
// кратковременная недоступность базы превращалась в постоянную недоступность API.
// После исчерпания попыток падаем громко: значит дело не в задержке старта.
static async Task EnsureDatabaseCreatedWithRetriesAsync(WebApplication application)
{
    const int maximumAttempts = 10;
    var delayBeforeNextAttempt = TimeSpan.FromSeconds(1);
    var applicationStopping = application.Lifetime.ApplicationStopping;

    for (var attemptNumber = 1; ; ++attemptNumber)
    {
        try
        {
            using var startupServiceScope = application.Services.CreateScope();
            var applicationDatabaseContext = startupServiceScope.ServiceProvider
                .GetRequiredService<OrderManagement.Infrastructure.Persistence.ApplicationDatabaseContext>();
            await applicationDatabaseContext.Database.EnsureCreatedAsync(applicationStopping);
            return;
        }
        catch (Exception databaseException) when (attemptNumber < maximumAttempts)
        {
            StartupLogging.LogDatabaseUnavailable(
                application.Logger,
                attemptNumber,
                maximumAttempts,
                delayBeforeNextAttempt,
                databaseException);

            await Task.Delay(delayBeforeNextAttempt, applicationStopping);

            // Нарастающая задержка с потолком: не выжигаем попытки за первые секунды,
            // но и не растягиваем старт до бесконечности.
            delayBeforeNextAttempt = TimeSpan.FromSeconds(
                Math.Min(delayBeforeNextAttempt.TotalSeconds * 2, 15));
        }
    }
}

internal static partial class StartupLogging
{
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "База данных недоступна (попытка {AttemptNumber} из {MaximumAttempts}), повтор через {RetryDelay}")]
    internal static partial void LogDatabaseUnavailable(
        ILogger logger,
        int attemptNumber,
        int maximumAttempts,
        TimeSpan retryDelay,
        Exception exception);
}
