using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using Xunit;

namespace OrderManagement.UnitTests.Infrastructure;

public sealed class OrderPersistenceModelTests
{
    [Fact]
    public void OrderStatus_НастроенКакМаркерОптимистичнойКонкуренции()
    {
        var contextOptions = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test;Password=model_test")
            .Options;
        using var databaseContext = new ApplicationDatabaseContext(
            contextOptions,
            Substitute.For<IPublisher>());

        var statusProperty = databaseContext.Model
            .FindEntityType(typeof(Order))
            ?.FindProperty(nameof(Order.Status));

        Assert.NotNull(statusProperty);
        Assert.True(statusProperty.IsConcurrencyToken);
    }
}
