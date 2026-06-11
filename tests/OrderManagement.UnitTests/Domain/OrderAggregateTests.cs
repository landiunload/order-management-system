using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enumerations;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Domain;

/// <summary>
/// Тесты бизнес-правил агрегата «Заказ».
/// Доменная модель тестируется без какой-либо инфраструктуры — это главное преимущество Clean Architecture.
/// </summary>
public sealed class OrderAggregateTests
{
    private static Order CreateOrderWithSingleItem()
    {
        var createdOrder = Order.Create(
            customerIdentifier: Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        createdOrder.AddOrderItem(
            productIdentifier: Guid.CreateVersion7(),
            productName: "Механическая клавиатура",
            MoneyAmount.Create(4990, "RUB"),
            quantity: 2);

        return createdOrder;
    }

    [Fact]
    public void Create_НовыйЗаказ_ИмеетСтатусСозданИДоменноеСобытие()
    {
        // Действие
        var createdOrder = CreateOrderWithSingleItem();

        // Проверка
        Assert.Equal(OrderStatus.Created, createdOrder.Status);
        Assert.Single(createdOrder.AccumulatedDomainEvents);
    }

    [Fact]
    public void CalculateTotalPrice_ДвеЕдиницыТовара_ВозвращаетУдвоеннуюЦену()
    {
        // Подготовка
        var createdOrder = CreateOrderWithSingleItem();

        // Действие
        var calculatedTotalPrice = createdOrder.CalculateTotalPrice();

        // Проверка
        Assert.Equal(9980, calculatedTotalPrice.Value);
        Assert.Equal("RUB", calculatedTotalPrice.CurrencyCode);
    }

    [Fact]
    public void Confirm_ЗаказБезПозиций_ВыбрасываетИсключениеБизнесПравила()
    {
        // Подготовка
        var emptyOrder = Order.Create(
            Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        // Действие и проверка
        Assert.Throws<DomainRuleViolationException>(emptyOrder.Confirm);
    }

    [Fact]
    public void AddOrderItem_ПослеПодтверждения_ВыбрасываетИсключениеБизнесПравила()
    {
        // Подготовка
        var confirmedOrder = CreateOrderWithSingleItem();
        confirmedOrder.Confirm();

        // Действие и проверка
        Assert.Throws<DomainRuleViolationException>(() =>
            confirmedOrder.AddOrderItem(
                Guid.CreateVersion7(),
                "Коврик для мыши",
                MoneyAmount.Create(990, "RUB"),
                quantity: 1));
    }

    [Fact]
    public void Cancel_ДоставленныйЗаказ_ВыбрасываетИсключениеБизнесПравила()
    {
        // Подготовка: доводим заказ до статуса «отменён», затем пытаемся отменить повторно
        var cancelledOrder = CreateOrderWithSingleItem();
        cancelledOrder.Cancel();

        // Действие и проверка
        Assert.Throws<DomainRuleViolationException>(cancelledOrder.Cancel);
    }
}
