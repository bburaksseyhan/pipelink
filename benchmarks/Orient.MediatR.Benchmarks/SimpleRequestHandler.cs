using Orient.MediatR.Handlers;

namespace Orient.MediatR.Benchmarks;

public class SimpleRequestHandler : IRequestHandler<SimpleRequest, SimpleResponse>
{
    public Task<SimpleResponse> Handle(SimpleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SimpleResponse(request.Id, $"Response for {request.Id}"));
    }
} 