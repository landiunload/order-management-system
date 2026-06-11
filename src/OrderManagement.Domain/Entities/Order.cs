using OrderManagement.Domain.Common;
using OrderManagement.Domain.Enumerations;
using OrderManagement.Domain.Events;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Domain.Entities;

/// <summary>
/// Агрегат «Заказ» — корневая сущность предметной области.
/// Все изменения позиций и статусов проходят только через методы агрегата,
/// поэтому заказ невозможно привести в противоречивое состояние извне
/// (инкапсуляция инвариантов — основа Domain-Driven Design).
/// </summary>
public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _orderItems = [];

    /// <summary>Идентификатор покупателя, оформившего заказ.</summary>
    public Guid CustomerIdentifier { get; private set; }

    /// <summary>Текущий статус жизненного цикла заказа.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Адрес, по которому нужно доставить заказ.</summary>
    public DeliveryAddress DeliveryAddress { get; private set; }

    /// <summary>Момент создания заказа в формате UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Позиции заказа. Коллекция только для чтения — изменение возможно лишь через методы агрегата.</summary>
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // Приватный конструктор без параметров требуется Entity Framework Core
    private Order()
    {
        DeliveryAddress = DeliveryAddress.Create("-", "-", "-");
    }

    private Order(Guid customerIdentifier, DeliveryAddress deliveryAddress)
    {
        CustomerIdentifier = customerIdentifier;
        DeliveryAddress = deliveryAddress;
        Status = OrderStatus.Created;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Создаёт новый заказ и публикует соответствующее доменное событие.</summary>
    public static Order Create(Guid customerIdentifier, DeliveryAddress deliveryAddress)
    {
        var createdOrder = new Order(customerIdentifier, deliveryAddress);
        createdOrder.RaiseDomainEvent(new OrderCreatedDomainEvent(createdOrder.Identifier, customerIdentifier));
        return createdOrder;
    }

    /// <summary>Добавляет позицию в заказ. Разрешено только до подтверждения заказа.</summary>
    public void AddOrderItem(Guid productIdentifier, string productName, MoneyAmount unitPrice, int quantity)
    {
        EnsureOrderIsEditable();
        _orderItems.Add(OrderItem.Create(productIdentifier, productName, unitPrice, quantity));
    }

    /// <summary>Подтверждает заказ. Пустой заказ подтвердить нельзя.</summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Created)
        {
            throw new DomainRuleViolationException($"Подтвердить можно только новый заказ. Текущий статус: {Status}.");
        }

        if (_orderItems.Count == 0)
        {
            throw new DomainRuleViolationException("Нельзя подтвердить заказ без единой позиции.");
        }

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Identifier));
    }

    /// <summary>Отменяет заказ. Доставленный заказ отменить нельзя.</summary>
    public void Cancel()
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new DomainRuleViolationException($"Заказ в статусе «{Status}» отменить нельзя.");
        }

        Status = OrderStatus.Cancelled;
    }

    /// <summary>Полная стоимость заказа — сумма стоимостей всех позиций.</summary>
    public MoneyAmount CalculateTotalPrice()
    {
        if (_orderItems.Count == 0)
        {
            return MoneyAmount.Create(0, "RUB");
        }

        return _orderItems
            .Select(orderItem => orderItem.CalculateTotalPrice())
            .Aggregate((accumulatedTotal, itemTotal) => accumulatedTotal.Add(itemTotal));
    }

    private void EnsureOrderIsEditable()
    {
        if (Status != OrderStatus.Created)
        {
            throw new DomainRuleViolationException($"Изменять состав заказа можно только до подтверждения. Текущий статус: {Status}.");
        }
    }
}
