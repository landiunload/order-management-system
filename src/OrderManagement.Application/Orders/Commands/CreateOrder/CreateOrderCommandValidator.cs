using FluentValidation;

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
            .WithMessage("Город доставки обязателен.");

        RuleFor(command => command.DeliveryStreetLine)
            .NotEmpty()
            .WithMessage("Улица и дом обязательны.");

        RuleFor(command => command.DeliveryPostalCode)
            .NotEmpty()
            .WithMessage("Почтовый индекс обязателен.");

        RuleFor(command => command.OrderItems)
            .NotEmpty()
            .WithMessage("Заказ должен содержать хотя бы одну позицию.");

        RuleForEach(command => command.OrderItems).ChildRules(orderItemRules =>
        {
            orderItemRules.RuleFor(orderItem => orderItem.ProductName)
                .NotEmpty()
                .WithMessage("Название товара обязательно.");

            orderItemRules.RuleFor(orderItem => orderItem.UnitPriceValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Цена товара не может быть отрицательной.");

            orderItemRules.RuleFor(orderItem => orderItem.Quantity)
                .GreaterThan(0)
                .WithMessage("Количество товара должно быть положительным.");
        });
    }
}
