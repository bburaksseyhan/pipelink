using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly Dictionary<TRequest, TResponse> _cache = new();

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
