using Mediator;
using OrderManagement.Application.Orders.DataTransferObjects;

namespace OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;

/// <summary>Запрос «получить страницу заказов».</summary>
public sealed record GetOrdersWithPaginationQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<OrderResponse>>;
