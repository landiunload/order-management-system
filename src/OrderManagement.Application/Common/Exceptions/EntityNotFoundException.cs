namespace OrderManagement.Application.Common.Exceptions;

/// <summary>
/// Исключение «сущность не найдена».
/// Промежуточное программное обеспечение веб-слоя преобразует его в HTTP-ответ 404.
/// </summary>
public sealed class EntityNotFoundException(string entityName, Guid entityIdentifier)
    : Exception($"Сущность «{entityName}» с идентификатором «{entityIdentifier}» не найдена.");
