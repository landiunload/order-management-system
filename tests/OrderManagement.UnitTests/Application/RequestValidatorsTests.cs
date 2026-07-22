using OrderManagement.Application.Orders.Commands.CreateOrder;
using OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;
using Xunit;

namespace OrderManagement.UnitTests.Application;

/// <summary>
/// Тесты валидаторов FluentValidation. Проверяют форму входных данных —
/// именно эти правила отсекают некорректные запросы до бизнес-логики,
/// и раньше они не были покрыты ни одним тестом.
/// </summary>
public sealed class RequestValidatorsTests
{
    private static CreateOrderCommand ValidCreateCommand() => new(
        CustomerIdentifier: Guid.CreateVersion7(),
        DeliveryCity: "Абакан",
        DeliveryStreetLine: "улица Ленина, дом 1",
        DeliveryPostalCode: "655000",
        OrderItems:
        [
            new CreateOrderItemRequest(Guid.CreateVersion7(), "Клавиатура", 4990m, "RUB", 1)
        ]);

    [Fact]
    public void CreateOrderValidator_КорректнаяКоманда_ПроходитВалидацию()
    {
        var result = new CreateOrderCommandValidator().Validate(ValidCreateCommand());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateOrderValidator_ПустойИдентификаторПокупателя_НеПроходит()
    {
        var command = ValidCreateCommand() with { CustomerIdentifier = Guid.Empty };
        Assert.False(new CreateOrderCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void CreateOrderValidator_ПустойГород_НеПроходит()
    {
        var command = ValidCreateCommand() with { DeliveryCity = "" };
        Assert.False(new CreateOrderCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void CreateOrderValidator_ЗаказБезПозиций_НеПроходит()
    {
        var command = ValidCreateCommand() with { OrderItems = [] };
        Assert.False(new CreateOrderCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void CreateOrderValidator_НулевоеКоличествоПозиции_НеПроходит()
    {
        var command = ValidCreateCommand() with
        {
            OrderItems = [new CreateOrderItemRequest(Guid.CreateVersion7(), "Клавиатура", 4990m, "RUB", 0)]
        };
        Assert.False(new CreateOrderCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void CreateOrderValidator_ОтрицательнаяЦенаПозиции_НеПроходит()
    {
        var command = ValidCreateCommand() with
        {
            OrderItems = [new CreateOrderItemRequest(Guid.CreateVersion7(), "Клавиатура", -1m, "RUB", 1)]
        };
        Assert.False(new CreateOrderCommandValidator().Validate(command).IsValid);
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(1, 1)]
    [InlineData(5, 100)]
    public void PaginationValidator_ДопустимыеГраницы_ПроходятВалидацию(int pageNumber, int pageSize)
    {
        var result = new GetOrdersWithPaginationQueryValidator()
            .Validate(new GetOrdersWithPaginationQuery(pageNumber, pageSize));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]   // номер страницы должен быть положительным
    [InlineData(-1, 20)]
    [InlineData(1, 0)]    // размер страницы ниже допустимого минимума
    [InlineData(1, 101)]  // размер страницы выше допустимого максимума
    public void PaginationValidator_НедопустимыеГраницы_НеПроходят(int pageNumber, int pageSize)
    {
        var result = new GetOrdersWithPaginationQueryValidator()
            .Validate(new GetOrdersWithPaginationQuery(pageNumber, pageSize));
        Assert.False(result.IsValid);
    }
}
