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
    using var startupServiceScope = webApplication.Services.CreateScope();
    var applicationDatabaseContext = startupServiceScope.ServiceProvider
        .GetRequiredService<OrderManagement.Infrastructure.Persistence.ApplicationDatabaseContext>();
    await applicationDatabaseContext.Database.EnsureCreatedAsync();
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
