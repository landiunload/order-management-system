using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Реализация хранилища заказов поверх Entity Framework Core.
/// Живёт в инфраструктурном слое и подменяема в тестах благодаря интерфейсу.
/// </summary>
public sealed class OrderRepository(ApplicationDatabaseContext databaseContext) : IOrderRepository
{
    /// <inheritdoc />
    public async Task<Order?> FindByIdentifierAsync(Guid orderIdentifier, CancellationToken cancellationToken)
    {
        return await databaseContext.Orders
            .AsTracking()
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Identifier == orderIdentifier, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Order?> FindByIdentifierAsNoTrackingAsync(Guid orderIdentifier, CancellationToken cancellationToken)
    {
        return await databaseContext.Orders
            .Include(order => order.OrderItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Identifier == orderIdentifier, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> FindPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var recordsToSkip = checked((pageNumber - 1) * pageSize);

        // Тай-брейк по идентификатору обязателен для страничной выборки: у двух заказов
        // может совпасть CreatedAtUtc, и тогда порядок между ними не определён — одна и
        // та же запись способна попасть на две соседние страницы сразу или не попасть
        // ни на одну. Guid v7 монотонен во времени, поэтому порядок остаётся смысловым.
        return await databaseContext.Orders
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Identifier)
            .Skip(recordsToSkip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Order order) => databaseContext.Orders.Add(order);
}
