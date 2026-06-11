namespace OrderManagement.Domain.Common;

/// <summary>
/// Маркерный интерфейс доменного события.
/// Доменные события позволяют сущностям сообщать об изменениях,
/// не создавая прямых зависимостей между слоями (принцип инверсии зависимостей из SOLID).
/// </summary>
public interface IDomainEvent
{
    /// <summary>Момент времени, когда событие произошло.</summary>
    DateTimeOffset OccurredAtUtc { get; }
}
