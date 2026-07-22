using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Domain;

/// <summary>
/// Тесты объекта-значения «адрес доставки».
/// Раньше этот объект-значение не был покрыт тестами вовсе, хотя защищает
/// сразу три обязательных поля и обрезает пробелы.
/// </summary>
public sealed class DeliveryAddressTests
{
    [Fact]
    public void Create_ПустойГород_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => DeliveryAddress.Create("  ", "улица Ленина, дом 1", "655000"));
    }

    [Fact]
    public void Create_ПустаяУлица_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => DeliveryAddress.Create("Абакан", "   ", "655000"));
    }

    [Fact]
    public void Create_ПустойИндекс_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", ""));
    }

    [Fact]
    public void Create_ПоляСПробелами_ОбрезаетКраевыеПробелы()
    {
        // Подготовка и действие
        var deliveryAddress = DeliveryAddress.Create(
            "  Абакан  ", "  улица Ленина, дом 1  ", "  655000  ");

        // Проверка
        Assert.Equal("Абакан", deliveryAddress.City);
        Assert.Equal("улица Ленина, дом 1", deliveryAddress.StreetLine);
        Assert.Equal("655000", deliveryAddress.PostalCode);
    }

    [Fact]
    public void Equals_ОдинаковыеПоля_АдресаРавны()
    {
        // Объект-значение сравнивается по содержимому, а не по ссылке
        var first = DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000");
        var second = DeliveryAddress.Create("Абакан", "улица Ленина, дом 1", "655000");

        Assert.Equal(first, second);
    }
}
