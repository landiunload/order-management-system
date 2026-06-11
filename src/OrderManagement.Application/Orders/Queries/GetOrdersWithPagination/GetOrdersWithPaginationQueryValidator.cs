using FluentValidation;

namespace OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;

/// <summary>Валидатор параметров постраничного запроса.</summary>
public sealed class GetOrdersWithPaginationQueryValidator : AbstractValidator<GetOrdersWithPaginationQuery>
{
    private const int MaximumAllowedPageSize = 100;

    public GetOrdersWithPaginationQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThan(0)
            .WithMessage("Номер страницы должен быть положительным.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaximumAllowedPageSize)
            .WithMessage($"Размер страницы должен быть от 1 до {MaximumAllowedPageSize}.");
    }
}
