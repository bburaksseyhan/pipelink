using Orient.MediatR.Handlers;
using Orient.MediatR.SampleWorker.Commands;

namespace Orient.MediatR.SampleWorker.CommandHandlers;

public sealed class ProcessTickCommandHandler : IRequestHandler<ProcessTickCommand, int>
{
    public Task<int> Handle(ProcessTickCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Tick * 2);
}
