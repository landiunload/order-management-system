using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.ValueObjects;

/// <summary>
/// Объект-значение «денежная сумма».
/// Неизменяемый (record), сравнивается по значению, запрещает отрицательные суммы
/// и операции над разными валютами — типичные ошибки при работе с «голым» decimal.
/// </summary>
public sealed record MoneyAmount
{
    /// <summary>Величина суммы.</summary>
    public decimal Value { get; }

    /// <summary>Трёхбуквенный код валюты по стандарту ISO 4217 (например, «RUB»).</summary>
    public string CurrencyCode { get; }

    private MoneyAmount(decimal value, string currencyCode)
    {
        Value = value;
        CurrencyCode = currencyCode;
    }

    /// <summary>Создаёт денежную сумму с проверкой бизнес-правил.</summary>
    public static MoneyAmount Create(decimal value, string currencyCode)
    {
        if (value < 0)
        {
            throw new DomainRuleViolationException("Денежная сумма не может быть отрицательной.");
        }

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
        {
            throw new DomainRuleViolationException("Код валюты должен состоять из трёх символов по стандарту ISO 4217.");
        }

        return new MoneyAmount(value, currencyCode.ToUpperInvariant());
    }

    /// <summary>Складывает две суммы, запрещая сложение разных валют.</summary>
    public MoneyAmount Add(MoneyAmount additionalAmount)
    {
        if (CurrencyCode != additionalAmount.CurrencyCode)
        {
            throw new DomainRuleViolationException("Нельзя складывать суммы в разных валютах.");
        }

        return new MoneyAmount(Value + additionalAmount.Value, CurrencyCode);
    }

    /// <summary>Умножает сумму на количество (например, цену на число единиц товара).</summary>
    public MoneyAmount MultiplyByQuantity(int quantity) => new(Value * quantity, CurrencyCode);
}
