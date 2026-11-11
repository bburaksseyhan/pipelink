using Orient.MediatR.Interfaces;
using Orient.MediatR.SampleApi.Features.Auth;
using Orient.MediatR.SampleApi.Features.Users;

namespace Orient.MediatR.SampleApi.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/user/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var user = await mediator.Send(new GetUserQuery(id), cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user);
        })
        .WithName("GetUserById");

        app.MapGet("/users/stream", (IMediator mediator, CancellationToken cancellationToken, int count = 10) =>
        {
            // IAsyncEnumerable is serialized as a streamed JSON array by ASP.NET Core.
            var users = mediator.SendStream(new StreamUsersQuery(count), cancellationToken);
            return Results.Ok(users);
        })
        .WithName("StreamUsers");

        app.MapPost("/login", async (LoginUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.Success ? Results.Ok(result) : Results.Unauthorized();
        })
        .WithName("LoginUser");

        app.MapPost("/users/{id:int}/created", async (int id, string email, IMediator mediator, CancellationToken cancellationToken) =>
        {
            // Demonstrates fan-out: both notification handlers run for a single publish.
            await mediator.Publish(new UserCreatedNotification(id, email), cancellationToken);
            return Results.Accepted();
        })
        .WithName("NotifyUserCreated");

        return app;
    }
}
