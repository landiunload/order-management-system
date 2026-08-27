using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories;

namespace OrderManagement.Infrastructure;

/// <summary>Регистрация служб инфраструктурного слоя в контейнере зависимостей.</summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует контекст базы данных, репозитории и единицу работы.</summary>
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("OrderManagementDatabase")
            ?? throw new InvalidOperationException(
                "Строка подключения «OrderManagementDatabase» не найдена в конфигурации.");

        serviceCollection.AddDbContext<ApplicationDatabaseContext>(databaseContextOptions =>
            databaseContextOptions
                .UseNpgsql(
                    databaseConnectionString,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null))
                // Запросы по умолчанию только читают. Сценарий изменения включает
                // tracking явно, что защищает новые query-методы от лишних снимков EF.
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        serviceCollection.AddScoped<IOrderRepository, OrderRepository>();
        serviceCollection.AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>();

        return serviceCollection;
    }
}
