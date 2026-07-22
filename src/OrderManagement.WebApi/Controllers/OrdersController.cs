using Mediator;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders.Commands.ConfirmOrder;
using OrderManagement.Application.Orders.Commands.CreateOrder;
using OrderManagement.Application.Orders.DataTransferObjects;
using OrderManagement.Application.Orders.Queries.GetOrderByIdentifier;
using OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;

namespace OrderManagement.WebApi.Controllers;

/// <summary>
/// Контроллер заказов — тонкий слой поверх Mediator.
/// Не содержит логики: только принимает HTTP-запрос, отправляет команду или запрос и возвращает результат.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender requestSender) : ControllerBase
{
    /// <summary>Создаёт новый заказ и возвращает его идентификатор.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrderAsync(
        [FromBody] CreateOrderCommand createOrderCommand,
        CancellationToken cancellationToken)
    {
        var createdOrderIdentifier = await requestSender.Send(createOrderCommand, cancellationToken);

        return CreatedAtAction(
            nameof(GetOrderByIdentifierAsync),
            new { orderIdentifier = createdOrderIdentifier },
            createdOrderIdentifier);
    }

    /// <summary>Возвращает заказ по идентификатору.</summary>
    [HttpGet("{orderIdentifier:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByIdentifierAsync(
        [FromRoute] Guid orderIdentifier,
        CancellationToken cancellationToken)
    {
        var foundOrder = await requestSender.Send(new GetOrderByIdentifierQuery(orderIdentifier), cancellationToken);
        return Ok(foundOrder);
    }

    /// <summary>Возвращает страницу заказов.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersPageAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ordersPage = await requestSender.Send(
            new GetOrdersWithPaginationQuery(pageNumber, pageSize),
            cancellationToken);

        return Ok(ordersPage);
    }

    /// <summary>Подтверждает заказ.</summary>
    [HttpPost("{orderIdentifier:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmOrderAsync(
        [FromRoute] Guid orderIdentifier,
        CancellationToken cancellationToken)
    {
        await requestSender.Send(new ConfirmOrderCommand(orderIdentifier), cancellationToken);
        return NoContent();
    }
}
