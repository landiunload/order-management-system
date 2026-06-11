namespace OrderManagement.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при нарушении бизнес-правила предметной области.
/// Отделяет ошибки бизнес-логики от технических ошибок инфраструктуры.
/// </summary>
public sealed class DomainRuleViolationException(string violationDescription)
    : Exception(violationDescription);
