using Depot.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Depot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        var handlerTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(IsHandlerInterface);

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, handlerType);
            }
        }

        return services;
    }

    private static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>);
    }
}
