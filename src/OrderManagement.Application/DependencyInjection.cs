using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Common.Behaviors;

namespace OrderManagement.Application;

/// <summary>
/// Регистрация служб слоя приложения в контейнере зависимостей.
/// Каждый слой регистрирует свои службы сам — точка входа лишь вызывает эти методы.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует шину сообщений, валидаторы и конвейерные behaviors.</summary>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection serviceCollection)
    {
        // Диспетчеризация генерируется на этапе компиляции: обработчики находит
        // генератор исходников в этой сборке, рефлексии в рантайме нет.
        // Время жизни обязательно Scoped: обработчики зависят от репозиториев, а те —
        // от DbContext, живущего в пределах запроса. При Singleton (значение по
        // умолчанию) контейнер отказался бы их построить.
        serviceCollection.AddMediator(mediatorOptions =>
            mediatorOptions.ServiceLifetime = ServiceLifetime.Scoped);

        serviceCollection.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Порядок важен: сначала логирование, затем валидация
        serviceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingPipelineBehavior<,>));
        serviceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationPipelineBehavior<,>));

        return serviceCollection;
    }
}
