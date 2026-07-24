using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace PipelinkTest.Api.Features.Users;

public record GetUserQuery(int UserId) : IRequest<UserDto?>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IUserRepository _users;

    public GetUserQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => _users.FindByIdAsync(request.UserId, cancellationToken);
}
