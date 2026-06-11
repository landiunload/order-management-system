using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.ValueObjects;

/// <summary>
/// Объект-значение «адрес доставки».
/// Гарантирует, что адрес всегда полон и корректен на момент создания.
/// </summary>
public sealed record DeliveryAddress
{
    /// <summary>Город доставки.</summary>
    public string City { get; }

    /// <summary>Улица, дом, квартира.</summary>
    public string StreetLine { get; }

    /// <summary>Почтовый индекс.</summary>
    public string PostalCode { get; }

    private DeliveryAddress(string city, string streetLine, string postalCode)
    {
        City = city;
        StreetLine = streetLine;
        PostalCode = postalCode;
    }

    /// <summary>Создаёт адрес доставки с проверкой обязательных полей.</summary>
    public static DeliveryAddress Create(string city, string streetLine, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainRuleViolationException("Город доставки обязателен.");
        }

        if (string.IsNullOrWhiteSpace(streetLine))
        {
            throw new DomainRuleViolationException("Улица и дом обязательны.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainRuleViolationException("Почтовый индекс обязателен.");
        }

        return new DeliveryAddress(city.Trim(), streetLine.Trim(), postalCode.Trim());
    }
}
