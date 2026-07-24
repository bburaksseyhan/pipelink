using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipelink.Interfaces;
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
    /// <remarks>
    /// Pipelink is registered as transient so that it resolves handlers from the scope it was created in,
    /// which makes scoped dependencies (such as EF Core DbContext) inside handlers work correctly.
    /// Inject <see cref="IPipelink"/> into your controllers, endpoints, or services.
    /// </remarks>
    public static IServiceCollection AddPipelink(
        this IServiceCollection services,
        Action<PipelinkConfiguration>? configureAction = null)
    {
        services.TryAddTransient<Implementation.Pipelink>();
        services.TryAddTransient<IPipelink>(sp => sp.GetRequiredService<Implementation.Pipelink>());

        var configuration = new PipelinkConfiguration(services);
        configureAction?.Invoke(configuration);
        configuration.RegisterServices();

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
