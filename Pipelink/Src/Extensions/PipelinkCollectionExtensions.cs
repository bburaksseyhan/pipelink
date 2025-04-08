using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;

namespace Pipelink.Extensions;

public static class PipelinkCollectionExtensions
{
    // Add the Mediator to DI container
    public static IServiceCollection AddPipelink(this IServiceCollection services)
    {
        services.AddSingleton<Pipelink.Implementation.Pipelink>(); // Register Pipelink
        return services;
    }

    // Add Request Handlers and Pipeline Behaviors to DI container
    public static IServiceCollection AddPipelinkHandlersAndBehaviors(this IServiceCollection services)
    {
        // Register all handlers in the assembly automatically (example: registering all IRequestHandler implementations)
        services.Scan(scan => scan
            .FromAssemblyOf<Pipelink.Implementation.Pipelink>() // Automatically scan the assembly where your Mediator resides
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // Register all notification handlers (e.g., INotificationHandler<UserCreatedNotification>)
        services.Scan(scan => scan
            .FromAssemblyOf<Pipelink.Implementation.Pipelink>()
            .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        
        // Register pipeline behaviors (example: logging, validation, caching)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }
}