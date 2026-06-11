using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Abstractions;

/// <summary>
/// Контракт хранилища заказов.
/// Слой предметной области объявляет интерфейс, а инфраструктура его реализует —
/// классическая инверсия зависимостей (буква «D» в SOLID).
/// </summary>
public interface IOrderRepository
{
    /// <summary>Возвращает заказ по идентификатору вместе с позициями или null, если заказ не найден.</summary>
    Task<Order?> FindByIdentifierAsync(Guid orderIdentifier, CancellationToken cancellationToken);

    /// <summary>Возвращает страницу заказов, отсортированных по дате создания (новые первыми).</summary>
    Task<IReadOnlyList<Order>> FindPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Регистрирует новый заказ для сохранения.</summary>
    void Add(Order order);
}
