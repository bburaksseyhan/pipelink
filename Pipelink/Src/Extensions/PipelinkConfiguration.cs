using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;

namespace Pipelink.Extensions;

/// <summary>
/// Configuration options for Pipelink.
/// </summary>
public class PipelinkConfiguration
{
    private readonly IServiceCollection _services;
    private readonly List<Assembly> _assemblies;

    internal PipelinkConfiguration(IServiceCollection services)
    {
        _services = services;
        _assemblies = new List<Assembly>();
    }

    /// <summary>
    /// Registers services from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">The type to use as a marker for the assembly to scan.</typeparam>
    /// <returns>The configuration instance for method chaining.</returns>
    public PipelinkConfiguration RegisterServicesFromAssemblyContaining<T>()
    {
        _assemblies.Add(typeof(T).Assembly);
        return this;
    }

    /// <summary>
    /// Registers services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for services.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    public PipelinkConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers services from multiple assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    public PipelinkConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        _assemblies.AddRange(assemblies);
        return this;
    }

    /// <summary>
    /// Registers an open generic pipeline behavior, e.g. <c>AddOpenBehavior(typeof(LoggingBehavior&lt;,&gt;))</c>.
    /// Behaviors run in registration order: the first registered behavior is the outermost.
    /// </summary>
    /// <param name="openBehaviorType">
    /// An open generic type implementing <see cref="IPipelineBehavior{TRequest,TResponse}"/>
    /// or <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/>.
    /// </param>
    /// <param name="lifetime">The service lifetime for the behavior. Defaults to transient.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    public PipelinkConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"{openBehaviorType.Name} must be an open generic type, e.g. typeof(LoggingBehavior<,>).", nameof(openBehaviorType));
        }

        var implementedInterfaces = openBehaviorType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .Where(i => i == typeof(IPipelineBehavior<,>) || i == typeof(IStreamPipelineBehavior<,>))
            .Distinct()
            .ToArray();

        if (implementedInterfaces.Length == 0)
        {
            throw new ArgumentException($"{openBehaviorType.Name} must implement IPipelineBehavior<,> or IStreamPipelineBehavior<,>.", nameof(openBehaviorType));
        }

        foreach (var serviceType in implementedInterfaces)
        {
            _services.Add(new ServiceDescriptor(serviceType, openBehaviorType, lifetime));
        }

        return this;
    }

    internal void RegisterServices()
    {
        if (!_assemblies.Any())
        {
            // If no assemblies specified, use the entry assembly
            _assemblies.Add(Assembly.GetEntryAssembly()!);
        }

        foreach (var assembly in _assemblies)
        {
            // Register request handlers
            _services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            // Register stream request handlers
            _services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IStreamRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            // Register notification handlers
            _services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());
        }
    }
}
