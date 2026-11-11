using System.Runtime.CompilerServices;
using Orient.MediatR.Handlers;

namespace Orient.MediatR.Benchmarks;

public class SimpleStreamRequestHandler : IStreamRequestHandler<SimpleStreamRequest, SimpleStreamResponse>
{
    public async IAsyncEnumerable<SimpleStreamResponse> Handle(SimpleStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new SimpleStreamResponse(i + 1, $"Response {i + 1}");
        }
    }
} 