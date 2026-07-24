using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace PipelinkTest.Api.Features.Auth;

public record LoginUserCommand(string Email, string Password) : IRequest<LoginResultDto>;

public record LoginResultDto(bool Success, string Email, string Token);

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // Simulate credential verification
        await Task.Delay(100, cancellationToken);

        var success = !string.IsNullOrWhiteSpace(request.Email) && !string.IsNullOrWhiteSpace(request.Password);
        var token = success ? Guid.NewGuid().ToString("N") : string.Empty;

        return new LoginResultDto(success, request.Email, token);
    }
}
