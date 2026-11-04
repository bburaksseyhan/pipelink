using Pipelink.Handlers;
using PipelinkTest.Worker.Commands;

namespace PipelinkTest.Worker.CommandHandlers;

public sealed class ProcessTickCommandHandler : IRequestHandler<ProcessTickCommand, int>
{
    public Task<int> Handle(ProcessTickCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Tick * 2);
}
