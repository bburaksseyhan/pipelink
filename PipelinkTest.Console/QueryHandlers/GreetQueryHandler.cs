using Pipelink.Handlers;
using PipelinkTest.ConsoleApp.Queries;

namespace PipelinkTest.ConsoleApp.QueryHandlers;

public sealed class GreetQueryHandler : IRequestHandler<GreetQuery, string>
{
    public Task<string> Handle(GreetQuery request, CancellationToken cancellationToken) =>
        Task.FromResult($"Hello, {request.Name}!");
}
