using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.ValueObjects;

/// <summary>
/// Объект-значение «адрес доставки».
/// Гарантирует, что адрес всегда полон и корректен на момент создания.
/// </summary>
public sealed record DeliveryAddress
{
    public const int MaximumCityLength = 128;
    public const int MaximumStreetLineLength = 256;
    public const int MaximumPostalCodeLength = 16;

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

        var normalizedCity = city.Trim();
        var normalizedStreetLine = streetLine.Trim();
        var normalizedPostalCode = postalCode.Trim();

        if (normalizedCity.Length > MaximumCityLength)
        {
            throw new DomainRuleViolationException(
                $"Город доставки не может быть длиннее {MaximumCityLength} символов.");
        }

        if (normalizedStreetLine.Length > MaximumStreetLineLength)
        {
            throw new DomainRuleViolationException(
                $"Адрес доставки не может быть длиннее {MaximumStreetLineLength} символов.");
        }

        if (normalizedPostalCode.Length > MaximumPostalCodeLength)
        {
            throw new DomainRuleViolationException(
                $"Почтовый индекс не может быть длиннее {MaximumPostalCodeLength} символов.");
        }

        return new DeliveryAddress(normalizedCity, normalizedStreetLine, normalizedPostalCode);
    }
}
