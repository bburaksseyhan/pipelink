using Pipelink.Interfaces;

namespace PipelinkTest.ConsoleApp.Queries;

public record GreetQuery(string Name) : IRequest<string>;
