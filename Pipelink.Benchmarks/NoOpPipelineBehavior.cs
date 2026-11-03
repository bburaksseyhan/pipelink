using Pipelink.Behaviors;
using Pipelink.Interfaces;

namespace Pipelink.Benchmarks;

/// <summary>
/// Silent pipeline step for benchmarking pipeline overhead without console I/O or caching side effects.
/// </summary>
public sealed class NoOpPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next) => next();
}
