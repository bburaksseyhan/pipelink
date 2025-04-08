using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

/// <summary>
/// Defines a pipeline behavior interface that allows processing and transforming of requests and responses
/// during the execution of a request handler in a pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled, which must implement the <see cref="IRequest{TResponse}"/> interface.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request in a pipeline and processes logic such as logging, validation, or caching before or after delegating to the next handler.
    /// </summary>
    /// <param name="request">The request object being processed in the pipeline.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing for cancellation of the task.</param>
    /// <param name="next">The next delegate in the pipeline to invoke the next step or handler for the request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is the response after the processing pipeline is complete.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
}

/// <summary>
/// Delegate representing a function that handles a request and produces a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
