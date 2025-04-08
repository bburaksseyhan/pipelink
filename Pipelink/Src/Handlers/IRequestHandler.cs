using Pipelink.Interfaces;

namespace Pipelink.Handlers;

/// <summary>
/// Represents a contract for handling requests of a specific type and producing a response of a specific type.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of response produced by handling the request.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request and produces a response asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request object to be processed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the response object.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
