using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Events;

/// <summary>Доменное событие «заказ подтверждён».</summary>
public sealed record OrderConfirmedDomainEvent(Guid OrderIdentifier) : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
