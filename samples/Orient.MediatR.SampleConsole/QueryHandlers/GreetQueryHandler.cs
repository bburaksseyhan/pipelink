using Orient.MediatR.Handlers;
using Orient.MediatR.SampleConsole.Queries;

namespace Orient.MediatR.SampleConsole.QueryHandlers;

public sealed class GreetQueryHandler : IRequestHandler<GreetQuery, string>
{
    public Task<string> Handle(GreetQuery request, CancellationToken cancellationToken) =>
        Task.FromResult($"Hello, {request.Name}!");
}
