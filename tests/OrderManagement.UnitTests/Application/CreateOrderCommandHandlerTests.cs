using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManagement.Application.Orders.Commands.CreateOrder;
using OrderManagement.Domain.Abstractions;
using OrderManagement.Domain.Entities;
using Xunit;

namespace OrderManagement.UnitTests.Application;

/// <summary>
/// Тесты обработчика команды создания заказа.
/// Зависимости подменяются интерфейсами — следствие принципа инверсии зависимостей.
/// </summary>
public sealed class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepositorySubstitute = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWorkSubstitute = Substitute.For<IUnitOfWork>();

    private CreateOrderCommandHandler CreateHandlerUnderTest() => new(
        _orderRepositorySubstitute,
        _unitOfWorkSubstitute,
        Substitute.For<ILogger<CreateOrderCommandHandler>>());

    private static CreateOrderCommand CreateValidCommand() => new(
        CustomerIdentifier: Guid.CreateVersion7(),
        DeliveryCity: "Абакан",
        DeliveryStreetLine: "улица Ленина, дом 1",
        DeliveryPostalCode: "655000",
        OrderItems:
        [
            new CreateOrderItemRequest(Guid.CreateVersion7(), "Механическая клавиатура", 4990, "RUB", 1)
        ]);

    [Fact]
    public async Task Handle_КорректнаяКоманда_ДобавляетЗаказИСохраняетИзменения()
    {
        // Подготовка
        var handlerUnderTest = CreateHandlerUnderTest();

        // Действие
        var createdOrderIdentifier = await handlerUnderTest.Handle(CreateValidCommand(), CancellationToken.None);

        // Проверка
        Assert.NotEqual(Guid.Empty, createdOrderIdentifier);
        _orderRepositorySubstitute.Received(1).Add(Arg.Any<Order>());
        await _unitOfWorkSubstitute.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_КомандаСДвумяПозициями_ЗаказСодержитОбеПозиции()
    {
        // Подготовка
        Order? capturedOrder = null;
        _orderRepositorySubstitute.Add(Arg.Do<Order>(addedOrder => capturedOrder = addedOrder));

        var commandWithTwoItems = CreateValidCommand() with
        {
            OrderItems =
            [
                new CreateOrderItemRequest(Guid.CreateVersion7(), "Механическая клавиатура", 4990, "RUB", 1),
                new CreateOrderItemRequest(Guid.CreateVersion7(), "Коврик для мыши", 990, "RUB", 3)
            ]
        };

        // Действие
        await CreateHandlerUnderTest().Handle(commandWithTwoItems, CancellationToken.None);

        // Проверка
        Assert.NotNull(capturedOrder);
        Assert.Equal(2, capturedOrder.OrderItems.Count);
        Assert.Equal(4990 + 990 * 3, capturedOrder.CalculateTotalPrice().Value);
    }
}
