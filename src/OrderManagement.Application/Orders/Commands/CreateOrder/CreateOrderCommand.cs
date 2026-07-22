using Mediator;

namespace OrderManagement.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Команда «создать заказ» (сторона записи в CQRS).
/// Содержит все данные, необходимые для создания заказа с позициями.
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerIdentifier,
    string DeliveryCity,
    string DeliveryStreetLine,
    string DeliveryPostalCode,
    IReadOnlyList<CreateOrderItemRequest> OrderItems) : IRequest<Guid>;

/// <summary>Позиция создаваемого заказа.</summary>
public sealed record CreateOrderItemRequest(
    Guid ProductIdentifier,
    string ProductName,
    decimal UnitPriceValue,
    string CurrencyCode,
    int Quantity);
