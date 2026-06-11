using OrderManagement.Domain.Common;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Domain.Entities;

/// <summary>
/// Позиция заказа: конкретный товар, его количество и цена на момент покупки.
/// Цена фиксируется в позиции, чтобы последующие изменения каталога не влияли на историю заказов.
/// </summary>
public sealed class OrderItem : BaseEntity
{
    /// <summary>Идентификатор товара из каталога.</summary>
    public Guid ProductIdentifier { get; private set; }

    /// <summary>Название товара на момент оформления заказа.</summary>
    public string ProductName { get; private set; }

    /// <summary>Цена за единицу товара на момент оформления заказа.</summary>
    public MoneyAmount UnitPrice { get; private set; }

    /// <summary>Количество единиц товара.</summary>
    public int Quantity { get; private set; }

    // Приватный конструктор без параметров требуется Entity Framework Core для материализации из базы данных
    private OrderItem()
    {
        ProductName = string.Empty;
        UnitPrice = MoneyAmount.Create(0, "RUB");
    }

    private OrderItem(Guid productIdentifier, string productName, MoneyAmount unitPrice, int quantity)
    {
        ProductIdentifier = productIdentifier;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>Создаёт позицию заказа с проверкой бизнес-правил.</summary>
    public static OrderItem Create(Guid productIdentifier, string productName, MoneyAmount unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainRuleViolationException("Название товара обязательно.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Количество товара должно быть положительным.");
        }

        return new OrderItem(productIdentifier, productName.Trim(), unitPrice, quantity);
    }

    /// <summary>Стоимость позиции: цена за единицу, умноженная на количество.</summary>
    public MoneyAmount CalculateTotalPrice() => UnitPrice.MultiplyByQuantity(Quantity);
}
