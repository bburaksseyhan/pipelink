using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;

namespace Orient.Pipelink.SampleApi.Features.Users;

public record StreamUsersQuery(int Count) : IStreamRequest<UserDto>;

public class StreamUsersQueryHandler : IStreamRequestHandler<StreamUsersQuery, UserDto>
{
    private readonly IUserRepository _users;

    public StreamUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public IAsyncEnumerable<UserDto> Handle(StreamUsersQuery request, CancellationToken cancellationToken)
        => _users.StreamAsync(request.Count, cancellationToken);
}
