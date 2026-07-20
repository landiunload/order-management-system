using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManagement.Application.Orders.Commands.ConfirmOrder;
using OrderManagement.Application.Orders.Queries.GetOrderByIdentifier;
using OrderManagement.Domain.Abstractions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Application;

/// <summary>
/// Тесты разделения путей чтения и записи при загрузке заказа по идентификатору.
/// Сторона запросов обязана брать заказ без отслеживания изменений, сторона команд —
/// с отслеживанием, иначе <c>SaveChangesAsync</c> не увидит правок агрегата.
/// </summary>
public sealed class OrderReadWritePathTests
{
    private readonly IOrderRepository _orderRepositorySubstitute = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWorkSubstitute = Substitute.For<IUnitOfWork>();

    private static Order CreateOrderWithSingleItem()
    {
        var createdOrder = Order.Create(
            customerIdentifier: Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        createdOrder.AddOrderItem(
            productIdentifier: Guid.CreateVersion7(),
            productName: "Механическая клавиатура",
            MoneyAmount.Create(4990, "RUB"),
            quantity: 1);

        return createdOrder;
    }

    [Fact]
    public async Task Запрос_ЗаказПоИдентификатору_ЧитаетБезОтслеживанияИзменений()
    {
        // Подготовка
        var existingOrder = CreateOrderWithSingleItem();
        _orderRepositorySubstitute
            .FindByIdentifierAsNoTrackingAsync(existingOrder.Identifier, Arg.Any<CancellationToken>())
            .Returns(existingOrder);

        var handlerUnderTest = new GetOrderByIdentifierQueryHandler(_orderRepositorySubstitute);

        // Действие
        var orderResponse = await handlerUnderTest.Handle(
            new GetOrderByIdentifierQuery(existingOrder.Identifier), CancellationToken.None);

        // Проверка
        Assert.Equal(existingOrder.Identifier, orderResponse.Identifier);
        await _orderRepositorySubstitute.Received(1)
            .FindByIdentifierAsNoTrackingAsync(existingOrder.Identifier, Arg.Any<CancellationToken>());
        await _orderRepositorySubstitute.DidNotReceive()
            .FindByIdentifierAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Команда_ПодтверждениеЗаказа_ЧитаетСОтслеживаниемИСохраняет()
    {
        // Подготовка
        var existingOrder = CreateOrderWithSingleItem();
        _orderRepositorySubstitute
            .FindByIdentifierAsync(existingOrder.Identifier, Arg.Any<CancellationToken>())
            .Returns(existingOrder);

        var handlerUnderTest = new ConfirmOrderCommandHandler(
            _orderRepositorySubstitute,
            _unitOfWorkSubstitute,
            Substitute.For<ILogger<ConfirmOrderCommandHandler>>());

        // Действие
        await handlerUnderTest.Handle(
            new ConfirmOrderCommand(existingOrder.Identifier), CancellationToken.None);

        // Проверка
        await _orderRepositorySubstitute.Received(1)
            .FindByIdentifierAsync(existingOrder.Identifier, Arg.Any<CancellationToken>());
        await _orderRepositorySubstitute.DidNotReceive()
            .FindByIdentifierAsNoTrackingAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWorkSubstitute.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
