using Mediator;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Common;

/// <summary>
/// Обёртка, превращающая доменное событие в нотификацию шины сообщений.
/// Слой домена остаётся свободным от внешних библиотек: события там — чистые маркеры
/// <see cref="IDomainEvent"/>, а адаптация происходит здесь.
/// Тип намеренно неуниверсальный: диспетчеризация генерируется на этапе компиляции,
/// и обобщённая обёртка потребовала бы отдельного обработчика на каждое событие.
/// Конкретное событие подписчик разбирает сопоставлением с образцом.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
