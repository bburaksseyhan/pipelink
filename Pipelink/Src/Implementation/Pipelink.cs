using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using Pipelink.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace Pipelink.Implementation;

/// <summary>
/// The Pipelink class provides a lightweight implementation for executing command-query responsibility segregation (CQRS)
/// patterns within applications. It acts as a mediator to send requests and publish notifications by delegating tasks
/// to their respective handlers and/or behaviors.
/// </summary>
/// <remarks>
/// Handlers and behaviors are resolved from the dependency injection container on every call, so their configured
/// lifetimes (transient, scoped, singleton) are fully respected. Only the strongly-typed dispatch shape is cached
/// per request/notification type, which keeps repeated dispatches allocation-friendly without capturing instances.
/// </remarks>
public sealed class Pipelink : IPipelink
{
    private readonly IServiceProvider _serviceProvider;

    // Caches hold compiled dispatch shapes keyed by request/notification type. They are static so that
    // transient/scoped Pipelink instances share the same cache. No handler instances are captured inside.
    private static readonly ConcurrentDictionary<Type, object> SendExecutorCache = new();
    private static readonly ConcurrentDictionary<Type, object> StreamExecutorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> PublishExecutorCache = new();

    public Pipelink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Handles the dispatching of a request to the corresponding handler and pipeline behaviors,
    /// and returns the response from the handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the handler for the given request.</typeparam>
    /// <param name="request">The request object implementing <see cref="IRequest{TResponse}"/> which needs to be handled.</param>
    /// <param name="cancellationToken">Optional cancellation token to propagate notifications that the operation should be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = (Func<IServiceProvider, object, CancellationToken, Task<TResponse>>)SendExecutorCache.GetOrAdd(
            request.GetType(),
            static requestType => CreateExecutor(nameof(CreateTypedSendExecutor), requestType, typeof(TResponse)));

        return executor(_serviceProvider, request, cancellationToken);
    }

    /// <summary>
    /// Sends a streaming request and returns a stream of responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of the responses in the stream.</typeparam>
    /// <param name="request">The streaming request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An asynchronous enumerable of responses.</returns>
    public IAsyncEnumerable<TResponse> SendStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = (Func<IServiceProvider, object, CancellationToken, IAsyncEnumerable<TResponse>>)StreamExecutorCache.GetOrAdd(
            request.GetType(),
            static requestType => CreateExecutor(nameof(CreateTypedStreamExecutor), requestType, typeof(TResponse)));

        return executor(_serviceProvider, request, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification to all registered notification handlers asynchronously.
    /// Handlers are invoked sequentially in registration order.
    /// </summary>
    /// <typeparam name="TNotification">
    /// The type of notification being published. Must implement the <see cref="INotification"/> interface.
    /// </typeparam>
    /// <param name="notification">The notification instance to be passed to the handlers.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous publish operation. Completion of this task indicates that all registered
    /// handlers for the notification have completed their processing.
    /// </returns>
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var executor = PublishExecutorCache.GetOrAdd(
            notification.GetType(),
            static notificationType =>
            {
                var method = typeof(Pipelink)
                    .GetMethod(nameof(CreateTypedPublishExecutor), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(notificationType);
                return (Func<IServiceProvider, object, CancellationToken, Task>)method.Invoke(null, null)!;
            });

        return executor(_serviceProvider, notification, cancellationToken);
    }

    private static object CreateExecutor(string factoryMethodName, Type requestType, Type responseType)
    {
        var method = typeof(Pipelink)
            .GetMethod(factoryMethodName, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType);
        return method.Invoke(null, null)!;
    }

    private static Func<IServiceProvider, object, CancellationToken, Task<TResponse>> CreateTypedSendExecutor<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        return static (serviceProvider, request, cancellationToken) =>
        {
            var typedRequest = (TRequest)request;
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, cancellationToken);

            // Wrap in reverse so the first registered behavior is the outermost (runs first).
            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var innerNext = next;
                next = () => behavior.Handle(typedRequest, cancellationToken, innerNext);
            }

            return next();
        };
    }

    private static Func<IServiceProvider, object, CancellationToken, IAsyncEnumerable<TResponse>> CreateTypedStreamExecutor<TRequest, TResponse>()
        where TRequest : IStreamRequest<TResponse>
    {
        return static (serviceProvider, request, cancellationToken) =>
        {
            var typedRequest = (TRequest)request;
            var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();

            StreamRequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, cancellationToken);

            var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>().ToArray();
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var innerNext = next;
                next = () => behavior.Handle(typedRequest, cancellationToken, innerNext);
            }

            return next();
        };
    }

    private static Func<IServiceProvider, object, CancellationToken, Task> CreateTypedPublishExecutor<TNotification>()
        where TNotification : INotification
    {
        return static async (serviceProvider, notification, cancellationToken) =>
        {
            var typedNotification = (TNotification)notification;

            foreach (var handler in serviceProvider.GetServices<INotificationHandler<TNotification>>())
            {
                await handler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
