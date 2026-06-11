using OrderManagement.Domain.Exceptions;
using OrderManagement.Domain.ValueObjects;
using Xunit;

namespace OrderManagement.UnitTests.Domain;

/// <summary>Тесты объекта-значения «денежная сумма».</summary>
public sealed class MoneyAmountTests
{
    [Fact]
    public void Create_ОтрицательнаяСумма_ВыбрасываетИсключениеБизнесПравила()
    {
        Assert.Throws<DomainRuleViolationException>(() => MoneyAmount.Create(-1, "RUB"));
    }

    [Fact]
    public void Add_РазныеВалюты_ВыбрасываетИсключениеБизнесПравила()
    {
        // Подготовка
        var amountInRubles = MoneyAmount.Create(100, "RUB");
        var amountInDollars = MoneyAmount.Create(100, "USD");

        // Действие и проверка
        Assert.Throws<DomainRuleViolationException>(() => amountInRubles.Add(amountInDollars));
    }

    [Fact]
    public void Equals_ОдинаковыеЗначения_СуммыРавны()
    {
        // Объекты-значения сравниваются по содержимому, а не по ссылке
        Assert.Equal(MoneyAmount.Create(100, "RUB"), MoneyAmount.Create(100, "rub"));
    }
}
