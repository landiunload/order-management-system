using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Events;

/// <summary>
/// Доменное событие «заказ создан».
/// На него могут подписаться обработчики уведомлений, аналитики и других побочных процессов,
/// не загрязняя при этом саму бизнес-логику создания заказа (принцип открытости/закрытости из SOLID).
/// </summary>
public sealed record OrderCreatedDomainEvent(Guid OrderIdentifier, Guid CustomerIdentifier) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
