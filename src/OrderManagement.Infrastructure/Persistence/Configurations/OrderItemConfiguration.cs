using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

/// <summary>Конфигурация таблицы позиций заказа.</summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderItem> orderItemBuilder)
    {
        orderItemBuilder.ToTable("order_items");

        orderItemBuilder.HasKey(orderItem => orderItem.Identifier);

        orderItemBuilder.Property(orderItem => orderItem.ProductIdentifier).IsRequired();

        orderItemBuilder.Property(orderItem => orderItem.ProductName)
            .HasMaxLength(256)
            .IsRequired();

        orderItemBuilder.Property(orderItem => orderItem.Quantity).IsRequired();

        // Объект-значение «денежная сумма» разворачивается в две колонки
        orderItemBuilder.ComplexProperty(orderItem => orderItem.UnitPrice, unitPriceBuilder =>
        {
            unitPriceBuilder.Property(moneyAmount => moneyAmount.Value)
                .HasColumnName("unit_price_value")
                .HasPrecision(18, 2)
                .IsRequired();

            unitPriceBuilder.Property(moneyAmount => moneyAmount.CurrencyCode)
                .HasColumnName("unit_price_currency_code")
                .HasMaxLength(3)
                .IsRequired();
        });

        orderItemBuilder.Ignore(orderItem => orderItem.AccumulatedDomainEvents);
    }
}
