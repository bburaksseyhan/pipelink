using Pipelink.Interfaces;

namespace PipelinkTest.Worker.Commands;

public record ProcessTickCommand(int Tick) : IRequest<int>;
