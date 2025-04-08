using Pipelink.Interfaces;

namespace Pipelink.Handlers;

public interface IRequestHandler<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
