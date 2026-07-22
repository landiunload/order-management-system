using Mediator;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

/// <summary>
/// Контекст базы данных приложения.
/// Помимо доступа к таблицам отвечает за публикацию доменных событий
/// после успешного сохранения изменений.
/// </summary>
public sealed class ApplicationDatabaseContext(
    DbContextOptions<ApplicationDatabaseContext> contextOptions,
    IPublisher domainEventPublisher) : DbContext(contextOptions)
{
    /// <summary>Таблица заказов.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Подключаем все конфигурации сущностей из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDatabaseContext).Assembly);
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Собираем доменные события до сохранения, чтобы не потерять их при очистке трекера
        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(entityEntry => entityEntry.Entity.AccumulatedDomainEvents.Count != 0)
            .Select(entityEntry => entityEntry.Entity)
            .ToList();

        var savedChangesCount = await base.SaveChangesAsync(cancellationToken);

        // Публикуем события только после успешной фиксации транзакции.
        // Доменное событие — чистый маркер IDomainEvent, поэтому перед публикацией
        // оборачиваем его в DomainEventNotification. Обёртка неуниверсальная, так что
        // рефлексия с Activator.CreateInstance, нужная прежней обобщённой версии,
        // больше не требуется — тип известен на этапе компиляции.
        foreach (var entityWithEvents in entitiesWithEvents)
        {
            foreach (var domainEvent in entityWithEvents.AccumulatedDomainEvents)
            {
                await domainEventPublisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
            }

            entityWithEvents.ClearAccumulatedDomainEvents();
        }

        return savedChangesCount;
    }
}
