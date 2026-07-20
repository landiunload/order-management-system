using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Abstractions;

/// <summary>
/// Контракт хранилища заказов.
/// Слой предметной области объявляет интерфейс, а инфраструктура его реализует —
/// классическая инверсия зависимостей (буква «D» в SOLID).
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Возвращает заказ по идентификатору вместе с позициями для изменения или null, если заказ не найден.
    /// Загружается с отслеживанием: возвращённый агрегат можно менять, изменения сохранит <c>IUnitOfWork</c>.
    /// Для чтения используйте <see cref="FindByIdentifierAsNoTrackingAsync"/>.
    /// </summary>
    Task<Order?> FindByIdentifierAsync(Guid orderIdentifier, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает заказ по идентификатору вместе с позициями только для чтения или null, если заказ не найден.
    /// Без отслеживания изменений: сторона запросов не платит за снимки состояния.
    /// </summary>
    Task<Order?> FindByIdentifierAsNoTrackingAsync(Guid orderIdentifier, CancellationToken cancellationToken);

    /// <summary>Возвращает страницу заказов, отсортированных по дате создания (новые первыми).</summary>
    Task<IReadOnlyList<Order>> FindPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Регистрирует новый заказ для сохранения.</summary>
    void Add(Order order);
}
