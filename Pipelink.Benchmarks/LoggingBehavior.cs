using Pipelink.Behaviors;
using Pipelink.Interfaces;

namespace Pipelink.Benchmarks;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        // Simulate logging
        await Task.Delay(1, cancellationToken);
        var response = await next();
        await Task.Delay(1, cancellationToken);
        return response;
    }
} 