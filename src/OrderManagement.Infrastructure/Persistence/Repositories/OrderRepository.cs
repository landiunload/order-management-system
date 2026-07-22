using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Abstractions;
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
        // Тай-брейк по идентификатору обязателен для страничной выборки: у двух заказов
        // может совпасть CreatedAtUtc, и тогда порядок между ними не определён — одна и
        // та же запись способна попасть на две соседние страницы сразу или не попасть
        // ни на одну. Guid v7 монотонен во времени, поэтому порядок остаётся смысловым.
        return await databaseContext.Orders
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Identifier)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Order order) => databaseContext.Orders.Add(order);
}
