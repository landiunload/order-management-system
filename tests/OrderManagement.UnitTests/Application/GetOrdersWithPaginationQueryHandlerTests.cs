using NSubstitute;
using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Application;

/// <summary>
/// Тесты обработчика постраничного запроса заказов.
/// Его тело целиком не было покрыто: мутационный прогон заменял его пустым блоком,
/// и ни один тест не падал — то есть параметры страницы могли не доезжать
/// до хранилища, а ответ мог оказаться пустым, и никто бы этого не заметил.
/// </summary>
public sealed class GetOrdersWithPaginationQueryHandlerTests
{
    private readonly IOrderRepository _orderRepositorySubstitute = Substitute.For<IOrderRepository>();

    private static Order CreateOrderWithSingleItem(string productName)
    {
        var createdOrder = Order.Create(
            customerIdentifier: Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        createdOrder.AddOrderItem(
            productIdentifier: Guid.CreateVersion7(),
            productName: productName,
            MoneyAmount.Create(4990, "RUB"),
            quantity: 1);

        return createdOrder;
    }

    [Fact]
    public async Task Запрос_СтраницаЗаказов_ПередаётПараметрыСтраницыВХранилище()
    {
        _orderRepositorySubstitute
            .FindPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handlerUnderTest = new GetOrdersWithPaginationQueryHandler(_orderRepositorySubstitute);

        await handlerUnderTest.Handle(
            new GetOrdersWithPaginationQuery(PageNumber: 3, PageSize: 25), CancellationToken.None);

        // Номер и размер страницы обязаны доехать до хранилища именно в этом порядке:
        // перепутанные местами аргументы дают тихо неверную выборку.
        await _orderRepositorySubstitute.Received(1)
            .FindPageAsync(3, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Запрос_СтраницаЗаказов_ВозвращаетВсеЗаказыСтраницыВТомЖеПорядке()
    {
        var firstOrder = CreateOrderWithSingleItem("Механическая клавиатура");
        var secondOrder = CreateOrderWithSingleItem("Монитор");

        _orderRepositorySubstitute
            .FindPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([firstOrder, secondOrder]);

        var handlerUnderTest = new GetOrdersWithPaginationQueryHandler(_orderRepositorySubstitute);

        var orderResponses = await handlerUnderTest.Handle(
            new GetOrdersWithPaginationQuery(), CancellationToken.None);

        Assert.Equal(2, orderResponses.Count);
        Assert.Equal(firstOrder.Identifier, orderResponses[0].Identifier);
        Assert.Equal(secondOrder.Identifier, orderResponses[1].Identifier);
        Assert.Equal("Механическая клавиатура", Assert.Single(orderResponses[0].OrderItems).ProductName);
    }

    [Fact]
    public async Task Запрос_ПустаяСтраница_ВозвращаетПустойСписокАНеNull()
    {
        _orderRepositorySubstitute
            .FindPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handlerUnderTest = new GetOrdersWithPaginationQueryHandler(_orderRepositorySubstitute);

        var orderResponses = await handlerUnderTest.Handle(
            new GetOrdersWithPaginationQuery(PageNumber: 99, PageSize: 10), CancellationToken.None);

        Assert.NotNull(orderResponses);
        Assert.Empty(orderResponses);
    }
}
