using Orient.Pipelink.Handlers;
using Orient.Pipelink.SampleConsole.Queries;

namespace Orient.Pipelink.SampleConsole.QueryHandlers;

public sealed class GreetQueryHandler : IRequestHandler<GreetQuery, string>
{
    public Task<string> Handle(GreetQuery request, CancellationToken cancellationToken) =>
        Task.FromResult($"Hello, {request.Name}!");
}
