using FluentValidation;

namespace OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;

/// <summary>Валидатор параметров постраничного запроса.</summary>
public sealed class GetOrdersWithPaginationQueryValidator : AbstractValidator<GetOrdersWithPaginationQuery>
{
    private const int MaximumAllowedPageSize = 100;
    private const int MaximumAllowedPageNumber = 10_000;

    public GetOrdersWithPaginationQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .InclusiveBetween(1, MaximumAllowedPageNumber)
            .WithMessage($"Номер страницы должен быть от 1 до {MaximumAllowedPageNumber}.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaximumAllowedPageSize)
            .WithMessage($"Размер страницы должен быть от 1 до {MaximumAllowedPageSize}.");
    }
}
