using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

/// <summary>Конфигурация таблицы заказов: ключи, объекты-значения, связи.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Identifier);

        builder.Property(order => order.CustomerIdentifier).IsRequired();

        // Статус храним строкой — читаемость базы важнее пары байтов
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            // Два параллельных перехода статуса не должны молча перетирать друг друга.
            // EF добавит исходный статус в WHERE и обнаружит конфликт по числу строк.
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(order => order.CreatedAtUtc).IsRequired();

        // Индекс повторяет устойчивый порядок постраничной выборки: дата и
        // идентификатор-тай-брейк обслуживаются одним индексным проходом.
        builder.HasIndex(order => new
        {
            order.CreatedAtUtc,
            order.Identifier
        });

        // Объект-значение «адрес доставки» разворачивается в колонки той же таблицы
        builder.ComplexProperty(order => order.DeliveryAddress, deliveryAddressBuilder =>
        {
            deliveryAddressBuilder.Property(address => address.City)
                .HasColumnName("delivery_city")
                .HasMaxLength(DeliveryAddress.MaximumCityLength)
                .IsRequired();

            deliveryAddressBuilder.Property(address => address.StreetLine)
                .HasColumnName("delivery_street_line")
                .HasMaxLength(DeliveryAddress.MaximumStreetLineLength)
                .IsRequired();

            deliveryAddressBuilder.Property(address => address.PostalCode)
                .HasColumnName("delivery_postal_code")
                .HasMaxLength(DeliveryAddress.MaximumPostalCodeLength)
                .IsRequired();
        });

        // Позиции заказа доступны только через агрегат, поэтому навигация настроена на приватное поле
        builder.HasMany(order => order.OrderItems)
            .WithOne()
            .HasForeignKey("order_identifier")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.OrderItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Доменные события в базе не хранятся
        builder.Ignore(order => order.AccumulatedDomainEvents);
    }
}
