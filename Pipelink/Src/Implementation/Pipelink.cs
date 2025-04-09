using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using Pipelink.Interfaces;

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
public class Pipelink
{
    private readonly IServiceProvider _serviceProvider;

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
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<IRequest<TResponse>, TResponse>>();
        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<IRequest<TResponse>, TResponse>>().ToList();

        async Task<TResponse> Handler() => await handler.Handle(request, cancellationToken);

        var pipeline = behaviors.Aggregate(
            (RequestHandlerDelegate<TResponse>)Handler,
            (next, pipeline) => () => pipeline.Handle(request, cancellationToken, next));

        return await pipeline();
    }

    /// <summary>
    /// Sends a streaming request and returns a stream of responses.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The streaming request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An asynchronous enumerable of responses.</returns>
    public IAsyncEnumerable<TResponse> SendStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IStreamRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        return handler.Handle(request, cancellationToken);
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
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("Handle");
            await (Task)method!.Invoke(handler, new object[] { notification, cancellationToken })!;
        }
    }
}