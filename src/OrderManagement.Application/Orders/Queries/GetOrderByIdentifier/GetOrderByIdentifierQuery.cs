using MediatR;
using OrderManagement.Application.Orders.DataTransferObjects;

namespace OrderManagement.Application.Orders.Queries.GetOrderByIdentifier;

/// <summary>Запрос «получить заказ по идентификатору» (сторона чтения в CQRS).</summary>
public sealed record GetOrderByIdentifierQuery(Guid OrderIdentifier) : IRequest<OrderResponse>;
