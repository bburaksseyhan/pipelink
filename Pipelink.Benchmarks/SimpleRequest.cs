using Pipelink.Interfaces;

namespace Pipelink.Benchmarks;

public record SimpleRequest(int Id) : IRequest<SimpleResponse>;

public record SimpleResponse(int Id, string Message); 