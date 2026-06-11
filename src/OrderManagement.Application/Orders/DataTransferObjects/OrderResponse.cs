namespace OrderManagement.Application.Orders.DataTransferObjects;

/// <summary>Ответ API: заказ со всеми позициями.</summary>
public sealed record OrderResponse(
    Guid Identifier,
    Guid CustomerIdentifier,
    string Status,
    string DeliveryCity,
    string DeliveryStreetLine,
    string DeliveryPostalCode,
    DateTimeOffset CreatedAtUtc,
    decimal TotalPriceValue,
    string CurrencyCode,
    IReadOnlyList<OrderItemResponse> OrderItems);
