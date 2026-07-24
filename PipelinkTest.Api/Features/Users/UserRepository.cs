namespace PipelinkTest.Api.Features.Users;

/// <summary>
/// A fake data source to demonstrate that Pipelink handlers can take
/// scoped dependencies through constructor injection.
/// </summary>
public interface IUserRepository
{
    Task<UserDto?> FindByIdAsync(int id, CancellationToken cancellationToken);
    IAsyncEnumerable<UserDto> StreamAsync(int count, CancellationToken cancellationToken);
}

public class InMemoryUserRepository : IUserRepository
{
    private static readonly string[] Names = ["Ada Lovelace", "Grace Hopper", "Alan Turing", "Barbara Liskov", "Dennis Ritchie"];

    public async Task<UserDto?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        // Simulate I/O latency
        await Task.Delay(50, cancellationToken);

        if (id <= 0)
        {
            return null;
        }

        var name = Names[(id - 1) % Names.Length];
        return new UserDto(id, name, $"user{id}@example.com");
    }

    public async IAsyncEnumerable<UserDto> StreamAsync(int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 1; i <= count; i++)
        {
            await Task.Delay(50, cancellationToken);
            yield return new UserDto(i, Names[(i - 1) % Names.Length], $"user{i}@example.com");
        }
    }
}
