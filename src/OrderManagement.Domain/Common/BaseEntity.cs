namespace OrderManagement.Domain.Common;

/// <summary>
/// Базовый класс для всех сущностей предметной области.
/// Инкапсулирует идентификатор и механизм накопления доменных событий.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _accumulatedDomainEvents = [];

    /// <summary>Уникальный идентификатор сущности.</summary>
    public Guid Identifier { get; protected set; } = Guid.CreateVersion7();

    /// <summary>Доменные события, накопленные сущностью с момента загрузки.</summary>
    public IReadOnlyCollection<IDomainEvent> AccumulatedDomainEvents => _accumulatedDomainEvents.AsReadOnly();

    /// <summary>Добавляет доменное событие в очередь на публикацию.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _accumulatedDomainEvents.Add(domainEvent);

    /// <summary>Очищает очередь доменных событий после их публикации.</summary>
    public void ClearAccumulatedDomainEvents() => _accumulatedDomainEvents.Clear();
}
