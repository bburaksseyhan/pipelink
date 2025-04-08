using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        // Example: You can add validation logic here.
        if (request == null)
            throw new ArgumentException("[VALIDATION] Request cannot be null");

        return await next();
    }
}
