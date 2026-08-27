using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

/// <summary>Конфигурация таблицы позиций заказа.</summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(orderItem => orderItem.Identifier);

        builder.Property(orderItem => orderItem.ProductIdentifier).IsRequired();

        builder.Property(orderItem => orderItem.ProductName)
            .HasMaxLength(OrderItem.MaximumProductNameLength)
            .IsRequired();

        builder.Property(orderItem => orderItem.Quantity).IsRequired();

        // Объект-значение «денежная сумма» разворачивается в две колонки
        builder.ComplexProperty(orderItem => orderItem.UnitPrice, unitPriceBuilder =>
        {
            unitPriceBuilder.Property(moneyAmount => moneyAmount.Value)
                .HasColumnName("unit_price_value")
                .HasPrecision(18, MoneyAmount.MaximumDecimalPlaces)
                .IsRequired();

            unitPriceBuilder.Property(moneyAmount => moneyAmount.CurrencyCode)
                .HasColumnName("unit_price_currency_code")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Ignore(orderItem => orderItem.AccumulatedDomainEvents);
    }
}
