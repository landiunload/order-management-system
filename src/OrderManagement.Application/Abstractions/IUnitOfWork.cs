namespace OrderManagement.Application.Abstractions;

/// <summary>Граница атомарного сохранения изменений сценария приложения.</summary>
public interface IUnitOfWork
{
    /// <summary>Фиксирует накопленные изменения и публикует доменные события.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
