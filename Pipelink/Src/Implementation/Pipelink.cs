using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Implementation;

/// <summary>
/// Mediator for CQRS request/notification/stream dispatch with an optional behavior pipeline.
/// </summary>
public sealed class Pipelink : IPipelink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatchCache _cache;

    /// <summary>
    /// Creates a mediator with its own dispatch cache. Prefer resolving <see cref="Pipelink"/> from a container
    /// configured with <c>AddPipelink</c>, which shares one cache per container and resolves handlers from the current scope.
    /// </summary>
    public Pipelink(IServiceProvider serviceProvider)
        : this(serviceProvider, new DispatchCache())
    {
    }

    internal Pipelink(IServiceProvider serviceProvider, DispatchCache cache)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (_cache.RequestWrappers.GetOrAdd(requestType, static t => CreateWrapper<TResponse>(t)) is not RequestWrapper<TResponse> wrapper)
        {
            // A request type implementing IRequest<T> for several T needs a response-specific wrapper.
            wrapper = (RequestWrapper<TResponse>)_cache.MultiResponseWrappers.GetOrAdd(
                (requestType, typeof(TResponse)),
                static k => CreateWrapper<TResponse>(k.RequestType));
        }

        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    private static object CreateWrapper<TResponse>(Type requestType) =>
        Activator.CreateInstance(typeof(RequestWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!;

    /// <inheritdoc />
    public async IAsyncEnumerable<TResponse> SendStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

        var enumerable = (IAsyncEnumerable<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        await foreach (var response in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return response;
        }
    }

    /// <inheritdoc />
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        foreach (var handler in _serviceProvider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Per-container dispatch state shared by all <see cref="Pipelink"/> instances resolved from that container.
    /// </summary>
    internal sealed class DispatchCache
    {
        public ConcurrentDictionary<Type, object> RequestWrappers { get; } = new();

        public ConcurrentDictionary<(Type RequestType, Type ResponseType), object> MultiResponseWrappers { get; } = new();
    }

    private abstract class RequestWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class RequestWrapper<TRequest, TResponse> : RequestWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int UNKNOWN = 0;
        private const int NO_BEHAVIORS = 1;
        private const int HAS_BEHAVIORS = 2;

        // Only the presence of behaviors is cached; instances are resolved per call to honor their DI lifetimes.
        private int _behaviorState = UNKNOWN;

        public override Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;

            if (Volatile.Read(ref _behaviorState) == NO_BEHAVIORS)
            {
                return serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                    .Handle(typedRequest, cancellationToken);
            }

            var resolved = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
            var behaviors = resolved as IPipelineBehavior<TRequest, TResponse>[] ?? resolved.ToArray();
            Volatile.Write(ref _behaviorState, behaviors.Length == 0 ? NO_BEHAVIORS : HAS_BEHAVIORS);

            RequestHandlerDelegate<TResponse> next = () =>
                serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                    .Handle(typedRequest, cancellationToken);

            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var inner = next;
                next = () => behavior.Handle(typedRequest, cancellationToken, inner);
            }

            return next();
        }
    }
}
