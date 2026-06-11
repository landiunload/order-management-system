namespace OrderManagement.Domain.Enumerations;

/// <summary>
/// Статус жизненного цикла заказа.
/// Переходы между статусами контролируются методами сущности <see cref="Entities.Order"/>,
/// что гарантирует невозможность некорректных переходов.
/// </summary>
public enum OrderStatus
{
    /// <summary>Заказ создан, но ещё не подтверждён покупателем.</summary>
    Created = 0,

    /// <summary>Заказ подтверждён и передан в обработку.</summary>
    Confirmed = 1,

    /// <summary>Заказ передан в службу доставки.</summary>
    Shipped = 2,

    /// <summary>Заказ доставлен покупателю.</summary>
    Delivered = 3,

    /// <summary>Заказ отменён.</summary>
    Cancelled = 4
}
