using Pipelink.Interfaces;

namespace Pipelink.Behaviors;

/// <summary>
/// Represents a pipeline behavior that performs validation on the incoming request
/// before passing it to the next handler in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request object.</typeparam>
/// <typeparam name="TResponse">The type of the response object.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// Handles the pipeline behavior for validating the incoming request before proceeding to the next behavior or handler in the pipeline.
    /// This method ensures that the provided request meets specific validation criteria before execution.
    /// <param name="request">The incoming request that requires processing.</param>
    /// <param name="cancellationToken">A cancellation token that allows processing to be canceled.</param>
    /// <param name="next">The next delegate in the pipeline to process the request.</param>
    /// <returns>Returns the response associated with the processed request.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided request is null.</exception>
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        // Example: You can add validation logic here.
        if (request == null)
            throw new ArgumentException("[VALIDATION] Request cannot be null");

        return await next();
    }
}
