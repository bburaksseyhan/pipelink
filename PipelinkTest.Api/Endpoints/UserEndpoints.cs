using Pipelink.Interfaces;
using PipelinkTest.Api.Features.Auth;
using PipelinkTest.Api.Features.Users;

namespace PipelinkTest.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/user/{id:int}", async (int id, IPipelink pipelink, CancellationToken cancellationToken) =>
        {
            var user = await pipelink.Send(new GetUserQuery(id), cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        })
        .WithName("GetUserById")
        .WithOpenApi();

        app.MapGet("/users/stream", (IPipelink pipelink, CancellationToken cancellationToken, int count = 10) =>
        {
            // IAsyncEnumerable is serialized as a streamed JSON array by ASP.NET Core.
            var users = pipelink.SendStream(new StreamUsersQuery(count), cancellationToken);
            return Results.Ok(users);
        })
        .WithName("StreamUsers")
        .WithOpenApi();

        app.MapPost("/login", async (LoginUserCommand command, IPipelink pipelink, CancellationToken cancellationToken) =>
        {
            var result = await pipelink.Send(command, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.Unauthorized();
        })
        .WithName("LoginUser")
        .WithOpenApi();

        app.MapPost("/users/{id:int}/created", async (int id, string email, IPipelink pipelink, CancellationToken cancellationToken) =>
        {
            // Demonstrates fan-out: both notification handlers run for a single publish.
            await pipelink.Publish(new UserCreatedNotification(id, email), cancellationToken);
            return Results.Accepted();
        })
        .WithName("NotifyUserCreated")
        .WithOpenApi();

        return app;
    }
}
