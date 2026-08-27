using Mediator;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders.DataTransferObjects;

namespace OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;

/// <summary>Обработчик постраничного запроса заказов.</summary>
public sealed class GetOrdersWithPaginationQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersWithPaginationQuery, IReadOnlyList<OrderResponse>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<OrderResponse>> Handle(
        GetOrdersWithPaginationQuery query,
        CancellationToken cancellationToken)
    {
        var ordersPage = await orderRepository.FindPageAsync(query.PageNumber, query.PageSize, cancellationToken);

        return ordersPage
            .Select(order => order.ToOrderResponse())
            .ToList();
    }
}
