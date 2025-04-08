using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;

namespace Pipelink.Extensions;

/// <summary>
/// Provides extension methods for configuring and managing dependency injection
/// for the Pipelink library in applications.
/// </summary>
public static class PipelinkCollectionExtensions
{
    // Add the Mediator to DI container
    /// <summary>
    /// Registers the Pipelink implementation in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the Pipelink service to.</param>
    /// <returns>The updated service collection with the Pipelink service registered.</returns>
    public static IServiceCollection AddPipelink(this IServiceCollection services)
    {
        services.AddSingleton<Pipelink.Implementation.Pipelink>(); // Register Pipelink
        return services;
    }

    // Add Request Handlers and Pipeline Behaviors to DI container
    /// <summary>
    /// Registers all request handlers, notification handlers, and pipeline behaviors in the dependency injection container.
    /// This method scans the assembly containing the Pipelink implementation and automatically registers
    /// the components implementing respective interfaces for request handling, notification handling, and pipeline behaviors.
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> to which the handlers and behaviors will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance including the registered handlers and behaviors.</returns>
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