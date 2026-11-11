using Orient.MediatR.Interfaces;

namespace Orient.MediatR.Benchmarks;

public record SimpleRequest(int Id) : IRequest<SimpleResponse>;

public record SimpleResponse(int Id, string Message); 