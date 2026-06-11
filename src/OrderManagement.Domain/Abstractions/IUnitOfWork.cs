namespace OrderManagement.Domain.Abstractions;

/// <summary>
/// Единица работы: атомарно фиксирует все накопленные изменения в базе данных
/// и публикует доменные события сохранённых сущностей.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Сохраняет все изменения текущей бизнес-операции в одной транзакции.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
