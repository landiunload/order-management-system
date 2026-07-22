using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Domain;

/// <summary>
/// Тесты позиции заказа. Бизнес-правила <see cref="OrderItem.Create"/> раньше
/// проверялись только косвенно через счастливый путь <c>Order.AddOrderItem</c>;
/// сами охранные условия оставались без прямого покрытия.
/// </summary>
public sealed class OrderItemTests
{
    private static MoneyAmount Price(decimal value = 100m) => MoneyAmount.Create(value, "RUB");

    [Fact]
    public void Create_НулевоеКоличество_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => OrderItem.Create(Guid.CreateVersion7(), "Клавиатура", Price(), quantity: 0));
    }

    [Fact]
    public void Create_ОтрицательноеКоличество_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => OrderItem.Create(Guid.CreateVersion7(), "Клавиатура", Price(), quantity: -1));
    }

    [Fact]
    public void Create_ПустоеНазвание_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => OrderItem.Create(Guid.CreateVersion7(), "   ", Price(), quantity: 1));
    }

    [Fact]
    public void Create_НазваниеСПробелами_ОбрезаетКраевыеПробелы()
    {
        var orderItem = OrderItem.Create(
            Guid.CreateVersion7(), "  Клавиатура  ", Price(), quantity: 1);

        Assert.Equal("Клавиатура", orderItem.ProductName);
    }

    [Fact]
    public void CalculateTotalPrice_ЦенаНаКоличество_ВозвращаетПроизведение()
    {
        var orderItem = OrderItem.Create(
            Guid.CreateVersion7(), "Клавиатура", Price(4990m), quantity: 3);

        var total = orderItem.CalculateTotalPrice();

        Assert.Equal(14970m, total.Value);
        Assert.Equal("RUB", total.CurrencyCode);
    }
}
