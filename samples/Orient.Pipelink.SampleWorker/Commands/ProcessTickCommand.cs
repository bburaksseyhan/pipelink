using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.SampleWorker.Commands;

public record ProcessTickCommand(int Tick) : IRequest<int>;
