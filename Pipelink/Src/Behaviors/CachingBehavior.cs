using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

/// <summary>
/// The CachingBehavior class is a pipeline behavior that implements request-response caching.
/// It checks whether the response for a given request is already cached, and if so, returns the cached response.
/// If the response is not cached, it processes the request, caches the response, and then returns it.
/// </summary>
/// <typeparam name="TRequest">The type of the request. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response associated with the request.</typeparam>
/// <remarks>
/// This behavior can improve performance by minimizing repeated processing for identical requests
/// across the pipeline. It stores responses in memory, and thus, the cache lifecycle is limited
/// by the application's runtime.
/// </remarks>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// A static dictionary used to cache responses for requests of type <typeparamref name="TRequest"/>
    /// to avoid redundant processing. The key is the request object of type <typeparamref name="TRequest"/>
    /// and the value is the corresponding response of type <typeparamref name="TResponse"/>.
    /// This helps in improving performance by reusing already computed responses for the same requests.
    /// </summary>
    private static readonly Dictionary<TRequest, TResponse> _cache = new();

    /// <summary>
    /// Handles the processing of the pipeline behavior with caching capabilities.
    /// If a cached response for the given request is available, it returns the cached response.
    /// Otherwise, it executes the next behavior or handler in the pipeline
    /// and stores the response in the cache for future use.
    /// </summary>
    /// <param name="request">The request object being handled.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="next">The delegate representing the next action in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation, containing the response of type TResponse.</returns>
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (_cache.TryGetValue(request, out var cachedResponse))
        {
            Console.WriteLine("[CACHE] Returning cached response");
            return cachedResponse;
        }

        var response = await next();
        _cache[request] = response;
        return response;
    }
}
