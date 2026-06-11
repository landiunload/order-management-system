using MediatR;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Common;

/// <summary>
/// Обёртка, превращающая доменное событие в нотификацию MediatR.
/// Слой домена остаётся свободным от внешних библиотек: события там — чистые маркеры
/// <see cref="IDomainEvent"/>, а адаптация к MediatR происходит здесь.
/// Обработчики подписываются как INotificationHandler&lt;DomainEventNotification&lt;КонкретноеСобытие&gt;&gt;.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
