using System.Runtime.CompilerServices;
using Pipelink.Handlers;
using PipelinkTest.Api.Dtos;
using PipelinkTest.Api.Queries;

namespace PipelinkTest.Api.Stream;

public class StreamUserQueryHandler : IStreamRequestHandler<StreamUserQuery, UserDto>
{
    public async IAsyncEnumerable<UserDto> Handle(StreamUserQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            // Simulate data fetching
            await Task.Delay(100, cancellationToken);
            yield return new UserDto { Id = i + 1, Name = $"User {i + 1}" };
        }
    }
} 