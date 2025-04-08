using Pipelink.Interfaces;
using PipelinkTest.Api.Dtos;

namespace PipelinkTest.Api.Queries;

public record StreamUserQuery(int Count) : IStreamRequest<UserDto>; 