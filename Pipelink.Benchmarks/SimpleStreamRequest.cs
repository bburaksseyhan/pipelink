using Pipelink.Interfaces;

namespace Pipelink.Benchmarks;

public record SimpleStreamRequest(int Count) : IStreamRequest<SimpleStreamResponse>;

public record SimpleStreamResponse(int Id, string Message); 