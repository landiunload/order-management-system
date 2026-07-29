using FluentValidation;
using Mediator;
using OrderManagement.Application.Common.Behaviors;
using OrderManagement.Application.Orders.DataTransferObjects;
using OrderManagement.Application.Orders.Queries.GetOrdersWithPagination;
using Xunit;

namespace OrderManagement.UnitTests.Application;

/// <summary>
/// Тесты конвейерного behavior проверки запросов.
/// Сами валидаторы покрыты отдельно, но это проверка не той стороны: она говорит,
/// что правила умеют находить ошибку, и молчит о том, мешает ли эта ошибка запросу
/// дойти до обработчика. Мутационный прогон показал, что <c>throw</c> в behavior
/// можно удалить целиком, и ни один из 50 тестов не падал. Здесь проверяется именно
/// то, ради чего валидаторы существуют: негодный запрос до обработчика не доходит.
/// </summary>
public sealed class RequestValidationPipelineBehaviorTests
{
    /// <summary>Считает вызовы следующего звена конвейера.</summary>
    private sealed class NextHandlerSpy
    {
        public int CallCount { get; private set; }

        public ValueTask<IReadOnlyList<OrderResponse>> HandleAsync(
            GetOrdersWithPaginationQuery query, CancellationToken cancellationToken)
        {
            ++CallCount;
            return ValueTask.FromResult<IReadOnlyList<OrderResponse>>([]);
        }
    }

    private static readonly GetOrdersWithPaginationQuery ValidQuery = new(PageNumber: 1, PageSize: 20);
    private static readonly GetOrdersWithPaginationQuery InvalidQuery = new(PageNumber: 0, PageSize: 0);

    private static (RequestValidationPipelineBehavior<GetOrdersWithPaginationQuery, IReadOnlyList<OrderResponse>> Behavior,
        NextHandlerSpy NextHandler) CreateBehavior(
            params IValidator<GetOrdersWithPaginationQuery>[] requestValidators)
    {
        var nextHandler = new NextHandlerSpy();
        var behavior = new RequestValidationPipelineBehavior<GetOrdersWithPaginationQuery, IReadOnlyList<OrderResponse>>(
            requestValidators);
        return (behavior, nextHandler);
    }

    [Fact]
    public async Task Behavior_ВалидаторовНет_ВызываетСледующийОбработчик()
    {
        var (behavior, nextHandler) = CreateBehavior();

        await behavior.Handle(ValidQuery, nextHandler.HandleAsync, CancellationToken.None);

        Assert.Equal(1, nextHandler.CallCount);
    }

    [Fact]
    public async Task Behavior_ЗапросПроходитВалидацию_ВызываетСледующийОбработчик()
    {
        var (behavior, nextHandler) = CreateBehavior(new GetOrdersWithPaginationQueryValidator());

        await behavior.Handle(ValidQuery, nextHandler.HandleAsync, CancellationToken.None);

        Assert.Equal(1, nextHandler.CallCount);
    }

    [Fact]
    public async Task Behavior_ЗапросНеПроходитВалидацию_ВыбрасываетИсключениеПроверки()
    {
        var (behavior, nextHandler) = CreateBehavior(new GetOrdersWithPaginationQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(InvalidQuery, nextHandler.HandleAsync, CancellationToken.None).AsTask());

        // Главное утверждение: обработчик не получил негодный запрос. Без него тест
        // проходил бы и в случае, когда behavior бросает исключение уже после вызова.
        Assert.Equal(0, nextHandler.CallCount);
    }

    [Fact]
    public async Task Behavior_НесколькоВалидаторовНедовольны_СобираетВсеОшибки()
    {
        var firstValidator = new InlineValidator<GetOrdersWithPaginationQuery>();
        firstValidator.RuleFor(query => query.PageNumber).GreaterThan(int.MaxValue - 1);

        var secondValidator = new InlineValidator<GetOrdersWithPaginationQuery>();
        secondValidator.RuleFor(query => query.PageSize).LessThan(int.MinValue + 1);

        var (behavior, nextHandler) = CreateBehavior(firstValidator, secondValidator);

        var validationException = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(ValidQuery, nextHandler.HandleAsync, CancellationToken.None).AsTask());

        // Ровно две, без повторов. Пока behavior отдавал всем валидаторам один
        // ValidationContext, тот накапливал ошибки в себе, и каждый валидатор
        // возвращал вдобавок к своей ещё и чужую — клиент получал четыре.
        Assert.Equal(2, validationException.Errors.Count());
        Assert.Equal(0, nextHandler.CallCount);
    }

    [Fact]
    public async Task Behavior_ОдинВалидаторДоволенДругойНет_ЗапросВсёРавноОтклонён()
    {
        var failingValidator = new InlineValidator<GetOrdersWithPaginationQuery>();
        failingValidator.RuleFor(query => query.PageNumber).GreaterThan(int.MaxValue - 1);

        var (behavior, nextHandler) = CreateBehavior(
            new GetOrdersWithPaginationQueryValidator(), failingValidator);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(ValidQuery, nextHandler.HandleAsync, CancellationToken.None).AsTask());

        Assert.Equal(0, nextHandler.CallCount);
    }
}
