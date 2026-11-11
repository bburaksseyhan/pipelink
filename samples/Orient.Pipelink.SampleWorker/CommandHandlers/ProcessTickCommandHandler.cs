using Orient.Pipelink.Handlers;
using Orient.Pipelink.SampleWorker.Commands;

namespace Orient.Pipelink.SampleWorker.CommandHandlers;

public sealed class ProcessTickCommandHandler : IRequestHandler<ProcessTickCommand, int>
{
    public Task<int> Handle(ProcessTickCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Tick * 2);
}
