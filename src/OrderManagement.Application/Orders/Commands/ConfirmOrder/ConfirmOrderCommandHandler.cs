using Mediator;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Commands.ConfirmOrder;

/// <summary>Обработчик команды подтверждения заказа.</summary>
public sealed partial class ConfirmOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmOrderCommandHandler> logger) : IRequestHandler<ConfirmOrderCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var foundOrder = await orderRepository.FindByIdentifierAsync(command.OrderIdentifier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Order), command.OrderIdentifier);

        // Все проверки допустимости перехода статуса выполняет сам агрегат
        foundOrder.Confirm();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogOrderConfirmed(logger, command.OrderIdentifier);

        // Mediator требует значение даже у команды без ответа: Unit — его «ничего»
        return Unit.Value;
    }

    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Information,
        Message = "Заказ {OrderIdentifier} подтверждён")]
    private static partial void LogOrderConfirmed(ILogger logger, Guid orderIdentifier);
}
