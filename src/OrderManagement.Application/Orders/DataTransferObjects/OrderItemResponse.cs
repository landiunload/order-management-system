namespace OrderManagement.Application.Orders.DataTransferObjects;

/// <summary>Ответ API: позиция заказа.</summary>
public sealed record OrderItemResponse(
    Guid Identifier,
    Guid ProductIdentifier,
    string ProductName,
    decimal UnitPriceValue,
    string CurrencyCode,
    int Quantity,
    decimal TotalPriceValue);
