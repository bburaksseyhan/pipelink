using System.Collections.Concurrent;
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
/// <para>
/// This behavior is opt-in: register it explicitly via <c>cfg.AddOpenBehavior(typeof(CachingBehavior&lt;,&gt;))</c>.
/// </para>
/// <para>
/// IMPORTANT: cache lookups rely on the request type's equality semantics. Use <c>record</c> requests (value equality)
/// for cache hits to work; plain classes fall back to reference equality and will never hit the cache.
/// The cache is process-wide, unbounded, and lives for the application's lifetime, so do not enable this behavior
/// for requests whose responses contain per-user or otherwise sensitive data.
/// </para>
/// </remarks>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<TRequest, TResponse> Cache = new();

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
        if (Cache.TryGetValue(request, out var cachedResponse))
        {
            return cachedResponse;
        }

        var response = await next().ConfigureAwait(false);
        Cache.TryAdd(request, response);
        return response;
    }
}
