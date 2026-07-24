using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace PipelinkTest.Api.Features.Users;

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
