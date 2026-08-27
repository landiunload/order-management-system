using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.ValueObjects;

/// <summary>
/// Объект-значение «денежная сумма».
/// Неизменяемый (record), сравнивается по значению, запрещает отрицательные суммы
/// и операции над разными валютами — типичные ошибки при работе с «голым» decimal.
/// </summary>
public sealed record MoneyAmount
{
    public const int MaximumDecimalPlaces = 2;
    public const decimal MaximumValue = 9_999_999_999_999_999.99m;

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
        if (value is < 0 or > MaximumValue)
        {
            throw new DomainRuleViolationException(
                $"Денежная сумма должна быть от 0 до {MaximumValue}.");
        }

        if (decimal.Round(value, MaximumDecimalPlaces) != value)
        {
            throw new DomainRuleViolationException(
                $"Денежная сумма не может содержать больше {MaximumDecimalPlaces} знаков после запятой.");
        }

        var normalizedCurrencyCode = currencyCode?.Trim().ToUpperInvariant();
        if (normalizedCurrencyCode is null ||
            normalizedCurrencyCode.Length != 3 ||
            normalizedCurrencyCode[0] is < 'A' or > 'Z' ||
            normalizedCurrencyCode[1] is < 'A' or > 'Z' ||
            normalizedCurrencyCode[2] is < 'A' or > 'Z')
        {
            throw new DomainRuleViolationException(
                "Код валюты должен состоять из трёх латинских букв по стандарту ISO 4217.");
        }

        return new MoneyAmount(value, normalizedCurrencyCode);
    }

    /// <summary>Складывает две суммы, запрещая сложение разных валют.</summary>
    public MoneyAmount Add(MoneyAmount additionalAmount)
    {
        ArgumentNullException.ThrowIfNull(additionalAmount);

        if (CurrencyCode != additionalAmount.CurrencyCode)
        {
            throw new DomainRuleViolationException("Нельзя складывать суммы в разных валютах.");
        }

        if (Value > MaximumValue - additionalAmount.Value)
        {
            throw new DomainRuleViolationException("Суммарная денежная сумма превышает допустимый предел.");
        }

        return new MoneyAmount(Value + additionalAmount.Value, CurrencyCode);
    }

    /// <summary>Умножает сумму на количество (например, цену на число единиц товара).</summary>
    public MoneyAmount MultiplyByQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Количество должно быть положительным.");
        }

        if (Value != 0 && quantity > MaximumValue / Value)
        {
            throw new DomainRuleViolationException("Суммарная денежная сумма превышает допустимый предел.");
        }

        return new MoneyAmount(Value * quantity, CurrencyCode);
    }
}
