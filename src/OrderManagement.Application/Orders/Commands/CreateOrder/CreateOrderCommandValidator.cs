using FluentValidation;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Валидатор команды создания заказа: проверяет форму входных данных
/// до того, как команда попадёт в бизнес-логику.
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerIdentifier)
            .NotEmpty()
            .WithMessage("Идентификатор покупателя обязателен.");

        RuleFor(command => command.DeliveryCity)
            .NotEmpty()
            .WithMessage("Город доставки обязателен.")
            .MaximumLength(DeliveryAddress.MaximumCityLength);

        RuleFor(command => command.DeliveryStreetLine)
            .NotEmpty()
            .WithMessage("Улица и дом обязательны.")
            .MaximumLength(DeliveryAddress.MaximumStreetLineLength);

        RuleFor(command => command.DeliveryPostalCode)
            .NotEmpty()
            .WithMessage("Почтовый индекс обязателен.")
            .MaximumLength(DeliveryAddress.MaximumPostalCodeLength);

        RuleFor(command => command.OrderItems)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Список позиций заказа обязателен.")
            .NotEmpty()
            .WithMessage("Заказ должен содержать хотя бы одну позицию.")
            .Must(orderItems => orderItems.Count <= Order.MaximumItemCount)
            .WithMessage($"Заказ не может содержать больше {Order.MaximumItemCount} позиций.");

        RuleForEach(command => command.OrderItems).ChildRules(orderItemRules =>
        {
            orderItemRules.RuleFor(orderItem => orderItem.ProductIdentifier)
                .NotEmpty()
                .WithMessage("Идентификатор товара обязателен.");

            orderItemRules.RuleFor(orderItem => orderItem.ProductName)
                .NotEmpty()
                .WithMessage("Название товара обязательно.")
                .MaximumLength(OrderItem.MaximumProductNameLength);

            orderItemRules.RuleFor(orderItem => orderItem.UnitPriceValue)
                .InclusiveBetween(0, MoneyAmount.MaximumValue)
                .WithMessage($"Цена товара должна быть от 0 до {MoneyAmount.MaximumValue}.")
                .PrecisionScale(18, MoneyAmount.MaximumDecimalPlaces, ignoreTrailingZeros: true)
                .WithMessage(
                    $"Цена товара не может содержать больше {MoneyAmount.MaximumDecimalPlaces} знаков после запятой.");

            orderItemRules.RuleFor(orderItem => orderItem.CurrencyCode)
                .NotEmpty()
                .WithMessage("Код валюты обязателен.")
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Код валюты должен состоять из трёх латинских букв.");

            orderItemRules.RuleFor(orderItem => orderItem.Quantity)
                .GreaterThan(0)
                .WithMessage("Количество товара должно быть положительным.");
        });
    }
}
