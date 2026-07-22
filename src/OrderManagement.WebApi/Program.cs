using OrderManagement.Application;
using OrderManagement.Infrastructure;
using OrderManagement.WebApi.Middleware;
using Serilog;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

// Структурированное логирование Serilog: настройки читаются из appsettings.json
webApplicationBuilder.Host.UseSerilog((hostBuilderContext, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(hostBuilderContext.Configuration));

// Каждый слой регистрирует свои зависимости самостоятельно
webApplicationBuilder.Services.AddApplicationLayer();
webApplicationBuilder.Services.AddInfrastructureLayer(webApplicationBuilder.Configuration);

// Сохраняем суффикс «Async» в именах действий, чтобы CreatedAtAction(nameof(...)) находил маршрут
webApplicationBuilder.Services.AddControllers(mvcOptions =>
    mvcOptions.SuppressAsyncSuffixInActionNames = false);
webApplicationBuilder.Services.AddEndpointsApiExplorer();
webApplicationBuilder.Services.AddSwaggerGen();

var webApplication = webApplicationBuilder.Build();

// В среде разработки создаём схему базы данных при старте;
// в производственной среде вместо этого применялись бы миграции EF Core
if (webApplication.Environment.IsDevelopment())
{
    await EnsureDatabaseCreatedWithRetriesAsync(webApplication);
}

// Глобальный перехват ошибок — единый формат ответов ProblemDetails для всех исключений
webApplication.UseMiddleware<GlobalExceptionHandlingMiddleware>();

webApplication.UseSerilogRequestLogging();

if (webApplication.Environment.IsDevelopment())
{
    webApplication.UseSwagger();
    webApplication.UseSwaggerUI();
}

webApplication.MapControllers();

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

    for (var attemptNumber = 1; ; ++attemptNumber)
    {
        try
        {
            using var startupServiceScope = application.Services.CreateScope();
            var applicationDatabaseContext = startupServiceScope.ServiceProvider
                .GetRequiredService<OrderManagement.Infrastructure.Persistence.ApplicationDatabaseContext>();
            await applicationDatabaseContext.Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception databaseException) when (attemptNumber < maximumAttempts)
        {
            application.Logger.LogWarning(
                databaseException,
                "База данных недоступна (попытка {НомерПопытки} из {ВсегоПопыток}), повтор через {Задержка}",
                attemptNumber,
                maximumAttempts,
                delayBeforeNextAttempt);

            await Task.Delay(delayBeforeNextAttempt);

            // Нарастающая задержка с потолком: не выжигаем попытки за первые секунды,
            // но и не растягиваем старт до бесконечности.
            delayBeforeNextAttempt = TimeSpan.FromSeconds(
                Math.Min(delayBeforeNextAttempt.TotalSeconds * 2, 15));
        }
    }
}
