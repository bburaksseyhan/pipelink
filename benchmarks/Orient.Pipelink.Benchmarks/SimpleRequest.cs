using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.Benchmarks;

public record SimpleRequest(int Id) : IRequest<SimpleResponse>;

public record SimpleResponse(int Id, string Message); 