using Mediator;

namespace OrderManagement.Application.Orders.Commands.ConfirmOrder;

/// <summary>Команда «подтвердить заказ».</summary>
public sealed record ConfirmOrderCommand(Guid OrderIdentifier) : IRequest;
