using Pipelink.Handlers;
using PipelinkTest.Api.Dtos;
using PipelinkTest.Api.Queries;

namespace PipelinkTest.Api.QueryHandlers;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Simulate data fetching
        await Task.Delay(100); 
        return new UserDto { Id = request.UserId, Name = "John Doe" };
    }
}
