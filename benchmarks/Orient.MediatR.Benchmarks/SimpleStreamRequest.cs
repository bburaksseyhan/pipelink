using Orient.MediatR.Interfaces;

namespace Orient.MediatR.Benchmarks;

public record SimpleStreamRequest(int Count) : IStreamRequest<SimpleStreamResponse>;

public record SimpleStreamResponse(int Id, string Message); 