using Orient.MediatR.Interfaces;

namespace Orient.MediatR.SampleConsole.Queries;

public record GreetQuery(string Name) : IRequest<string>;
