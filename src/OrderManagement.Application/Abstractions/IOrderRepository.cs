using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Abstractions;

/// <summary>Контракт хранилища заказов, необходимый сценариям приложения.</summary>
public interface IOrderRepository
{
    /// <summary>
    /// Возвращает заказ с позициями для изменения или <see langword="null"/>, если он не найден.
    /// Загруженный агрегат отслеживается текущей единицей работы.
    /// </summary>
    Task<Order?> FindByIdentifierAsync(Guid orderIdentifier, CancellationToken cancellationToken);

    /// <summary>Возвращает заказ с позициями только для чтения без отслеживания изменений.</summary>
    Task<Order?> FindByIdentifierAsNoTrackingAsync(Guid orderIdentifier, CancellationToken cancellationToken);

    /// <summary>Возвращает страницу заказов, отсортированных от новых к старым.</summary>
    Task<IReadOnlyList<Order>> FindPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Добавляет новый заказ в текущую единицу работы.</summary>
    void Add(Order order);
}
