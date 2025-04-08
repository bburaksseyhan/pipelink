using Pipelink.Handlers;
using PipelinkTest.Api.Commands;
using PipelinkTest.Api.Dtos;

namespace PipelinkTest.Api.CommandHandlers;

public record LoginUserCommandHandler : IRequestHandler<LoginUserCommand,LoginUserDto>
{
    public async Task<LoginUserDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(100);
        return new LoginUserDto()
        {
            Email = "burak.seyhan@commencis.com"
        };
    }
}