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

        // Register core pipeline behaviors
        _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.LoggingBehavior<,>));
        _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.ValidationBehavior<,>));
        _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.CachingBehavior<,>));
    }
} 