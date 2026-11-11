using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orient.MediatR.Interfaces;

namespace Orient.MediatR.Extensions;

/// <summary>
/// Registers Orient.MediatR in a dependency injection container.
/// </summary>
public static class OrientMediatRServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator, scans the configured assemblies for handlers and applies the configured behaviors.
    /// </summary>
    /// <param name="services">The service collection to add Orient.MediatR to.</param>
    /// <param name="configureAction">Optional configuration of assemblies and behaviors.</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>
    /// The mediator is registered as transient so that it resolves handlers from the scope it was created in,
    /// which makes scoped dependencies (such as EF Core DbContext) inside handlers work correctly.
    /// Inject <see cref="IMediator"/> into your controllers, endpoints, or services.
    /// </remarks>
    public static IServiceCollection AddOrientMediatR(
        this IServiceCollection services,
        Action<OrientMediatRConfiguration>? configureAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<Implementation.Mediator>();
        services.TryAddTransient<IMediator>(sp => sp.GetRequiredService<Implementation.Mediator>());
#pragma warning disable CS0618 // Keep IPipelink resolvable for code migrating from Pipelink 2.x.
        services.TryAddTransient<IPipelink>(sp => sp.GetRequiredService<Implementation.Mediator>());
#pragma warning restore CS0618

        var configuration = new OrientMediatRConfiguration(services);
        configureAction?.Invoke(configuration);
        configuration.RegisterServices();

        return services;
    }

    /// <summary>
    /// Migration bridge for code written against Pipelink 2.x. Same as <see cref="AddOrientMediatR"/>.
    /// </summary>
    [Obsolete("AddPipelink was renamed to AddOrientMediatR in Orient.MediatR. This bridge will be removed in the next major version.")]
    public static IServiceCollection AddPipelink(
        this IServiceCollection services,
        Action<OrientMediatRConfiguration>? configureAction = null) =>
        AddOrientMediatR(services, configureAction);
}
