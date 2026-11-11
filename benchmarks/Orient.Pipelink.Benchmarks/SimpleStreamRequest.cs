using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.Benchmarks;

public record SimpleStreamRequest(int Count) : IStreamRequest<SimpleStreamResponse>;

public record SimpleStreamResponse(int Id, string Message); 