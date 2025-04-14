using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Pipelink.Handlers;
using Pipelink.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Pipelink.Implementation;

/// <summary>
/// The Pipelink class provides a lightweight implementation for executing command-query responsibility segregation (CQRS)
/// patterns within applications. It acts as a mediator to send requests and publish notifications by delegating tasks
/// to their respective handlers and/or behaviors.
/// </summary>
/// <remarks>
/// Pipelink utilizes the provided dependency injection container to dynamically resolve and invoke the appropriate
/// request handlers, notification handlers, and pipeline behaviors at runtime. Handlers and behaviors should be registered
/// before using the functionality provided by this class.
/// </remarks>
public sealed class Pipelink : IPipelink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, Delegate> _handlerCache = new();
    private readonly ConcurrentDictionary<Type, object[]> _behaviorCache = new();
    private readonly ObjectPool<RequestContext> _requestContextPool;

    public Pipelink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var policy = new RequestContextPooledObjectPolicy();
        _requestContextPool = new DefaultObjectPool<RequestContext>(policy);
    }

    /// <summary>
    /// Handles the dispatching of a request to the corresponding handler and pipeline behaviors,
    /// and returns the response from the handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the handler for the given request.</typeparam>
    /// <param name="request">The request object implementing <see cref="IRequest{TResponse}"/> which needs to be handled.</param>
    /// <param name="cancellationToken">Optional cancellation token to propagate notifications that the operation should be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerDelegate = GetOrCreateHandlerDelegate<TResponse>(requestType);

        return await ((Func<object, CancellationToken, Task<TResponse>>)handlerDelegate)(
            request,
            cancellationToken
        );
    }

    private Delegate CreateHandlerDelegate<TResponse>(Type requestType)
    {
        var handler = _serviceProvider.GetRequiredService(typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse)));
        var handleMethod = handler.GetType().GetMethod("Handle") ?? throw new InvalidOperationException($"Handler for {requestType} does not implement Handle method");

        var requestParam = Expression.Parameter(typeof(object), "request");
        var tokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var handlerConst = Expression.Constant(handler);
        var requestCast = Expression.Convert(requestParam, requestType);

        var call = Expression.Call(handlerConst, handleMethod, requestCast, tokenParam);
        var lambda = Expression.Lambda<Func<object, CancellationToken, Task<TResponse>>>(call, requestParam, tokenParam);

        return lambda.Compile();
    }

    private Delegate GetOrCreateHandlerDelegate<TResponse>(Type requestType)
    {
        return _handlerCache.GetOrAdd(requestType, t => CreateHandlerDelegate<TResponse>(t));
    }

    /// <summary>
    /// Sends a streaming request and returns a stream of responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of the responses in the stream.</typeparam>
    /// <param name="request">The streaming request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An asynchronous enumerable of responses.</returns>
    public async IAsyncEnumerable<TResponse> SendStream<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handler = (Func<object, CancellationToken, IAsyncEnumerable<TResponse>>)_handlerCache.GetOrAdd(requestType, type =>
        {
            var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(type, typeof(TResponse));
            var handlerInstance = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException($"No handler found for request type {type.Name}");
            var handleMethod = handlerType.GetMethod("Handle") ?? throw new InvalidOperationException("Handle method not found");

            var requestParam = Expression.Parameter(typeof(object), "request");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var handlerConstant = Expression.Constant(handlerInstance);
            var requestConverted = Expression.Convert(requestParam, type);

            var methodCall = Expression.Call(handlerConstant, handleMethod, requestConverted, cancellationTokenParam);
            return Expression.Lambda<Func<object, CancellationToken, IAsyncEnumerable<TResponse>>>(methodCall, requestParam, cancellationTokenParam).Compile();
        });

        await foreach (var response in handler(request, cancellationToken))
        {
            yield return response;
        }
    }

    /// <summary>
    /// Publishes a notification to all registered notification handlers asynchronously.
    /// </summary>
    /// <typeparam name="TNotification">
    /// The type of notification being published. Must implement the <see cref="INotification"/> interface.
    /// </typeparam>
    /// <param name="notification">
    /// The notification instance to be passed to the handlers.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous publish operation. Completion of this task indicates that all registered
    /// handlers for the notification have completed their processing.
    /// </returns>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var handlers = _serviceProvider.GetServices(handlerType).ToArray();

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("Handle");
            if (method != null)
            {
                await ((Task)method.Invoke(handler, new object[] { notification, cancellationToken })!);
            }
        }
    }
}

internal sealed class RequestContext : IRequestContext
{
    public object Request { get; set; } = default!;
    public IServiceProvider ServiceProvider { get; set; } = default!;
    public CancellationToken CancellationToken { get; set; }
}

internal sealed class RequestContextPooledObjectPolicy : IPooledObjectPolicy<RequestContext>
{
    public RequestContext Create() => new();

    public bool Return(RequestContext obj)
    {
        obj.Request = default!;
        obj.ServiceProvider = default!;
        obj.CancellationToken = default;
        return true;
    }
}