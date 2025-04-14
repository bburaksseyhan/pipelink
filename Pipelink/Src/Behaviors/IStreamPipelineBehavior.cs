using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

/// <summary>
/// Defines a pipeline behavior interface that allows processing and transforming of streaming requests and responses
/// during the execution of a stream request handler in a pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled, which must implement the <see cref="IStreamRequest{TResponse}"/> interface.</typeparam>
/// <typeparam name="TResponse">The type of the responses in the stream returned by the handler.</typeparam>
public interface IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the streaming request in a pipeline and processes logic such as logging, validation, or caching before or after delegating to the next handler.
    /// </summary>
    /// <param name="request">The request object being processed in the pipeline.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing for cancellation of the task.</param>
    /// <param name="next">The next delegate in the pipeline to invoke the next step or handler for the request.</param>
    /// <returns>An asynchronous stream of responses after the processing pipeline is complete.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken, StreamRequestHandlerDelegate<TResponse> next);
}

/// <summary>
/// Delegate representing a function that handles a streaming request and produces a stream of responses.
/// </summary>
/// <typeparam name="TResponse">The type of the responses in the stream returned by the handler.</typeparam>
public delegate IAsyncEnumerable<TResponse> StreamRequestHandlerDelegate<TResponse>(); 