using MediatR;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Orders.DataTransferObjects;
using OrderManagement.Domain.Abstractions;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries.GetOrderByIdentifier;

/// <summary>Обработчик запроса заказа по идентификатору.</summary>
public sealed class GetOrderByIdentifierQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrderByIdentifierQuery, OrderResponse>
{
    /// <inheritdoc />
    public async Task<OrderResponse> Handle(GetOrderByIdentifierQuery query, CancellationToken cancellationToken)
    {
        var foundOrder = await orderRepository.FindByIdentifierAsNoTrackingAsync(query.OrderIdentifier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Order), query.OrderIdentifier);

        return foundOrder.ToOrderResponse();
    }
}
