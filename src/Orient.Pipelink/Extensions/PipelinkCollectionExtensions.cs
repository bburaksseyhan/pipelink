using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.Extensions;

/// <summary>
/// Registers Pipelink in a dependency injection container.
/// </summary>
public static class PipelinkCollectionExtensions
{
    /// <summary>
    /// Registers the mediator, scans the configured assemblies for handlers and applies the configured behaviors.
    /// </summary>
    /// <param name="services">The service collection to add Pipelink to.</param>
    /// <param name="configureAction">Optional configuration of assemblies and behaviors.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>
    /// Pipelink is registered as transient so that it resolves handlers from the scope it was created in,
    /// which makes scoped dependencies (such as EF Core DbContext) inside handlers work correctly.
    /// Inject <see cref="IPipelink"/> into your controllers, endpoints, or services.
    /// </remarks>
    public static IServiceCollection AddPipelink(
        this IServiceCollection services,
        Action<PipelinkConfiguration>? configureAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

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
