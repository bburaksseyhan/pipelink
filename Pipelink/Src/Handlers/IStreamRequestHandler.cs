using System.Collections.Generic;
using Pipelink.Interfaces;

namespace Pipelink.Handlers;

/// <summary>
/// Represents a contract for handling streaming requests of a specific type and producing a stream of responses.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle. Must implement <see cref="IStreamRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of response produced by handling the request.</typeparam>
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles a streaming request and produces a stream of responses asynchronously.
    /// </summary>
    /// <param name="request">The request object to be processed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable of response objects.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
} 