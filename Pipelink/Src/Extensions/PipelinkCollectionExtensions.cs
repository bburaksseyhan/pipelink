using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using System;

namespace Pipelink.Extensions;

/// <summary>
/// Provides extension methods for configuring and managing dependency injection
/// for the Pipelink library in applications.
/// </summary>
public static class PipelinkCollectionExtensions
{
    /// <summary>
    /// Registers the Pipelink implementation in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the Pipelink service to.</param>
    /// <param name="configureAction">Optional configuration action to configure Pipelink services.</param>
    /// <returns>The updated service collection with the Pipelink service registered.</returns>
    public static IServiceCollection AddPipelink(
        this IServiceCollection services,
        Action<PipelinkConfiguration>? configureAction = null)
    {
        // Register core Pipelink service
        services.AddSingleton<Implementation.Pipelink>();

        // Configure services if configuration action is provided
        if (configureAction != null)
        {
            var configuration = new PipelinkConfiguration(services);
            configureAction(configuration);
            configuration.RegisterServices();
        }
        else
        {
            // If no configuration provided, register services from the entry assembly
            var configuration = new PipelinkConfiguration(services);
            configuration.RegisterServices();
        }

        return services;
    }

    /// <summary>
    /// [Obsolete] Use AddPipelink with configuration instead.
    /// Registers all request handlers, notification handlers, and pipeline behaviors in the dependency injection container.
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> to which the handlers and behaviors will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance including the registered handlers and behaviors.</returns>
    [Obsolete("Use AddPipelink with configuration instead. This method will be removed in a future version.")]
    public static IServiceCollection AddPipelinkHandlersAndBehaviors(this IServiceCollection services)
    {
        return AddPipelink(services);
    }
}