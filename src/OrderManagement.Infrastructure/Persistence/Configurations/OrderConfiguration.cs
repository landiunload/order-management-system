using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

/// <summary>Конфигурация таблицы заказов: ключи, объекты-значения, связи.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> orderBuilder)
    {
        orderBuilder.ToTable("orders");

        orderBuilder.HasKey(order => order.Identifier);

        orderBuilder.Property(order => order.CustomerIdentifier).IsRequired();

        // Статус храним строкой — читаемость базы важнее пары байтов
        orderBuilder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        orderBuilder.Property(order => order.CreatedAtUtc).IsRequired();

        // Объект-значение «адрес доставки» разворачивается в колонки той же таблицы
        orderBuilder.ComplexProperty(order => order.DeliveryAddress, deliveryAddressBuilder =>
        {
            deliveryAddressBuilder.Property(address => address.City)
                .HasColumnName("delivery_city")
                .HasMaxLength(128)
                .IsRequired();

            deliveryAddressBuilder.Property(address => address.StreetLine)
                .HasColumnName("delivery_street_line")
                .HasMaxLength(256)
                .IsRequired();

            deliveryAddressBuilder.Property(address => address.PostalCode)
                .HasColumnName("delivery_postal_code")
                .HasMaxLength(16)
                .IsRequired();
        });

        // Позиции заказа доступны только через агрегат, поэтому навигация настроена на приватное поле
        orderBuilder.HasMany(order => order.OrderItems)
            .WithOne()
            .HasForeignKey("order_identifier")
            .OnDelete(DeleteBehavior.Cascade);

        orderBuilder.Navigation(order => order.OrderItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Доменные события в базе не хранятся
        orderBuilder.Ignore(order => order.AccumulatedDomainEvents);
    }
}
