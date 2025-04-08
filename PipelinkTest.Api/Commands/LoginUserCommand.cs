using Pipelink.Interfaces;
using PipelinkTest.Api.Dtos;

namespace PipelinkTest.Api.Commands;

public record LoginUserCommand(string Email) : IRequest<LoginUserDto>;