using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.DataTransferObjects;

/// <summary>
/// Преобразование доменных сущностей в ответы API.
/// Выделено в отдельный класс, чтобы обработчики не дублировали логику маппинга.
/// </summary>
public static class OrderMappingExtensions
{
    /// <summary>Преобразует агрегат «Заказ» в ответ API.</summary>
    public static OrderResponse ToOrderResponse(this Order order)
    {
        var totalPrice = order.CalculateTotalPrice();

        return new OrderResponse(
            order.Identifier,
            order.CustomerIdentifier,
            order.Status.ToString(),
            order.DeliveryAddress.City,
            order.DeliveryAddress.StreetLine,
            order.DeliveryAddress.PostalCode,
            order.CreatedAtUtc,
            totalPrice.Value,
            totalPrice.CurrencyCode,
            order.OrderItems
                .Select(orderItem => new OrderItemResponse(
                    orderItem.Identifier,
                    orderItem.ProductIdentifier,
                    orderItem.ProductName,
                    orderItem.UnitPrice.Value,
                    orderItem.UnitPrice.CurrencyCode,
                    orderItem.Quantity,
                    orderItem.CalculateTotalPrice().Value))
                .ToList());
    }
}
