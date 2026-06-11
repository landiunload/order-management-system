using OrderManagement.Domain.Abstractions;

namespace OrderManagement.Infrastructure.Persistence;

/// <summary>
/// Единица работы поверх контекста Entity Framework Core:
/// одна бизнес-операция — одна транзакция базы данных.
/// </summary>
public sealed class EntityFrameworkUnitOfWork(ApplicationDatabaseContext databaseContext) : IUnitOfWork
{
    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        databaseContext.SaveChangesAsync(cancellationToken);
}
