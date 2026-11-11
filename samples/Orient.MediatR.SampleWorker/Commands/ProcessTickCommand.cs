using Orient.MediatR.Interfaces;

namespace Orient.MediatR.SampleWorker.Commands;

public record ProcessTickCommand(int Tick) : IRequest<int>;
