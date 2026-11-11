using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.SampleConsole.Queries;

public record GreetQuery(string Name) : IRequest<string>;
