using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pipelink.Interfaces;

/// <summary>
/// Represents the main interface for the Pipelink mediator pattern implementation.
/// </summary>
public interface IPipelink
{
    /// <summary>
    /// Sends a request through the pipeline and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request.</typeparam>
    /// <param name="request">The request to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The response from the request handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming request through the pipeline and returns a stream of responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of responses in the stream.</typeparam>
    /// <param name="request">The streaming request to be processed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An asynchronous stream of responses.</returns>
    IAsyncEnumerable<TResponse> SendStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
} 