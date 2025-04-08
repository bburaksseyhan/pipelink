using Pipelink.Interfaces;
using PipelinkTest.Api.Dtos;

namespace PipelinkTest.Api.Queries;

public class GetUserQuery : IRequest<UserDto>
{
    public int UserId { get; set; }

    public GetUserQuery(int userId)
    {
        UserId = userId;
    }
}