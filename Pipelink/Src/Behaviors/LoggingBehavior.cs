using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        Console.WriteLine($"[LOG] Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[LOG] Handled {typeof(TRequest).Name}");
        return response;
    }
}
