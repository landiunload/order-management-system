using Mediator;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Обработчик команды создания заказа.
/// Оркестрирует доменную модель и сохранение, не содержа бизнес-правил —
/// они инкапсулированы в самом агрегате «Заказ».
/// </summary>
public sealed partial class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Guid>
{
    /// <inheritdoc />
    public async ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var deliveryAddress = DeliveryAddress.Create(
            command.DeliveryCity,
            command.DeliveryStreetLine,
            command.DeliveryPostalCode);

        var createdOrder = Order.Create(command.CustomerIdentifier, deliveryAddress);

        foreach (var orderItemRequest in command.OrderItems)
        {
            createdOrder.AddOrderItem(
                orderItemRequest.ProductIdentifier,
                orderItemRequest.ProductName,
                MoneyAmount.Create(orderItemRequest.UnitPriceValue, orderItemRequest.CurrencyCode),
                orderItemRequest.Quantity);
        }

        orderRepository.Add(createdOrder);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogOrderCreated(
            logger,
            createdOrder.Identifier,
            command.CustomerIdentifier,
            command.OrderItems.Count);

        return createdOrder.Identifier;
    }

    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Information,
        Message = "Создан заказ {OrderIdentifier} для покупателя {CustomerIdentifier} с {ItemCount} позициями")]
    private static partial void LogOrderCreated(
        ILogger logger,
        Guid orderIdentifier,
        Guid customerIdentifier,
        int itemCount);
}
