using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enumerations;
using OrderManagement.Domain.Events;
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

    [Fact]
    public void Confirm_ЗаказСПозициями_ПереходитВПодтверждёнИПубликуетСобытие()
    {
        // Подготовка
        var createdOrder = CreateOrderWithSingleItem();

        // Действие
        createdOrder.Confirm();

        // Проверка: помимо события создания появляется событие подтверждения
        Assert.Equal(OrderStatus.Confirmed, createdOrder.Status);
        Assert.Contains(createdOrder.AccumulatedDomainEvents,
            domainEvent => domainEvent is OrderConfirmedDomainEvent);
    }

    [Fact]
    public void Confirm_УжеПодтверждённыйЗаказ_ВыбрасываетИсключениеБизнесПравила()
    {
        // Подготовка
        var confirmedOrder = CreateOrderWithSingleItem();
        confirmedOrder.Confirm();

        // Действие и проверка: подтвердить можно только заказ в статусе «создан»
        Assert.Throws<DomainRuleViolationException>(confirmedOrder.Confirm);
    }

    [Fact]
    public void Cancel_НовыйЗаказ_ПереходитВОтменён()
    {
        // Подготовка
        var createdOrder = CreateOrderWithSingleItem();

        // Действие
        createdOrder.Cancel();

        // Проверка
        Assert.Equal(OrderStatus.Cancelled, createdOrder.Status);
    }

    [Fact]
    public void CalculateTotalPrice_ЗаказБезПозиций_ВозвращаетНоль()
    {
        // Подготовка: заказ без единой позиции — граничный случай пустой коллекции
        var emptyOrder = Order.Create(
            Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        // Действие
        var total = emptyOrder.CalculateTotalPrice();

        // Проверка
        Assert.Equal(0, total.Value);
        Assert.Equal("RUB", total.CurrencyCode);
    }

    [Fact]
    public void Create_ПустойИдентификаторПокупателя_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(() => Order.Create(
            Guid.Empty,
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000")));
    }

    [Fact]
    public void AddOrderItem_ДругаяВалюта_ВыбрасываетИсключениеБизнесПравила()
    {
        var order = CreateOrderWithSingleItem();

        Assert.Throws<DomainRuleViolationException>(() => order.AddOrderItem(
            Guid.CreateVersion7(),
            "Мышь",
            MoneyAmount.Create(1990m, "USD"),
            quantity: 1));
    }

    [Fact]
    public void AddOrderItem_СверхЛимита_ВыбрасываетИсключениеБизнесПравила()
    {
        var order = Order.Create(
            Guid.CreateVersion7(),
            DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000"));

        for (var itemIndex = 0; itemIndex < Order.MaximumItemCount; ++itemIndex)
        {
            order.AddOrderItem(
                Guid.CreateVersion7(),
                $"Товар {itemIndex}",
                MoneyAmount.Create(1m, "RUB"),
                quantity: 1);
        }

        Assert.Throws<DomainRuleViolationException>(() => order.AddOrderItem(
            Guid.CreateVersion7(),
            "Лишний товар",
            MoneyAmount.Create(1m, "RUB"),
            quantity: 1));
    }

    [Fact]
    public void КоллекцииТолькоДляЧтения_НеСоздаютсяПовторноПриКаждомОбращении()
    {
        var order = CreateOrderWithSingleItem();

        Assert.Same(order.OrderItems, order.OrderItems);
        Assert.Same(order.AccumulatedDomainEvents, order.AccumulatedDomainEvents);
    }
}
