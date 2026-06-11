using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Abstractions;
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
            databaseContextOptions.UseNpgsql(databaseConnectionString));

        serviceCollection.AddScoped<IOrderRepository, OrderRepository>();
        serviceCollection.AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>();

        return serviceCollection;
    }
}
