using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Abstractions;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Commands.ConfirmOrder;

/// <summary>Обработчик команды подтверждения заказа.</summary>
public sealed class ConfirmOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmOrderCommandHandler> logger) : IRequestHandler<ConfirmOrderCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var foundOrder = await orderRepository.FindByIdentifierAsync(command.OrderIdentifier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Order), command.OrderIdentifier);

        // Все проверки допустимости перехода статуса выполняет сам агрегат
        foundOrder.Confirm();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Заказ {ИдентификаторЗаказа} подтверждён", command.OrderIdentifier);
    }
}
