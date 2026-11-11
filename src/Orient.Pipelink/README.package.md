# Orient.Pipelink

A lightweight, MIT-licensed mediator for .NET and part of the Orient family. Pipelink implements the request/response, notification, and streaming patterns commonly used with CQRS, with a familiar MediatR-style API and a minimal dependency footprint.

Previously published as **Pipelink** (2.x).

## Features

- Request/response pattern (`IRequest<T>` / `IRequestHandler<,>`)
- Notification publishing to multiple handlers
- Streaming requests via `IAsyncEnumerable<T>`
- Opt-in pipeline behaviors for cross-cutting concerns
- Automatic handler registration through assembly scanning
- Handlers resolved per dispatch — transient and scoped lifetimes (e.g. EF Core `DbContext`) work correctly

## Installation

```bash
dotnet add package Orient.Pipelink
```

Requires .NET 10 (`net10.0`).

## Quick Start

```csharp
using Orient.Pipelink.Extensions;
using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;

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

## Migrating from Pipelink 2.x

Swap the `Pipelink` package for `Orient.Pipelink` and replace `using Pipelink.` with `using Orient.Pipelink.`. Type names (`IPipelink`, `AddPipelink`, `PipelinkConfiguration`, …) are unchanged.

## Documentation

For full documentation, the MediatR migration guide, and samples, visit the [GitHub repository](https://github.com/bburaksseyhan/pipelink).

## License

MIT
