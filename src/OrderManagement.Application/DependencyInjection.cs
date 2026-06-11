using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Common.Behaviors;

namespace OrderManagement.Application;

/// <summary>
/// Регистрация служб слоя приложения в контейнере зависимостей.
/// Каждый слой регистрирует свои службы сам — точка входа лишь вызывает эти методы.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Регистрирует MediatR, валидаторы и конвейерные behaviors.</summary>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        serviceCollection.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Порядок важен: сначала логирование, затем валидация
        serviceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingPipelineBehavior<,>));
        serviceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationPipelineBehavior<,>));

        return serviceCollection;
    }
}
