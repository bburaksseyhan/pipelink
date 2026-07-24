# Pipelink

A lightweight, MIT-licensed mediator for .NET implementing the request/response, notification, and streaming patterns commonly used with CQRS. Familiar MediatR-style API with a minimal dependency footprint.

## Features

- Request/response pattern (`IRequest<T>` / `IRequestHandler<,>`)
- Notification publishing to multiple handlers
- Streaming requests via `IAsyncEnumerable<T>`
- Opt-in pipeline behaviors for cross-cutting concerns
- Automatic handler registration through assembly scanning
- Handlers resolved per dispatch — transient and scoped lifetimes (e.g. EF Core `DbContext`) work correctly

## Installation

```bash
dotnet add package Pipelink
```

## Quick Start

```csharp
// Register (Program.cs)
builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});

// Define a request and its handler
public record GetUserQuery(int UserId) : IRequest<UserDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new UserDto(request.UserId, "Ada Lovelace"));
}

// Inject IPipelink anywhere and send
var user = await pipelink.Send(new GetUserQuery(1));
```

Pipeline behaviors are opt-in and run in registration order:

```csharp
builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

## Documentation

For full documentation, a MediatR migration guide, and samples, visit the [GitHub repository](https://github.com/bburaksseyhan/pipelink).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
