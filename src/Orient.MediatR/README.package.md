# Orient.MediatR

A lightweight, MIT-licensed mediator for .NET and part of the Orient family. It implements the request/response, notification, and streaming patterns commonly used with CQRS, with a familiar MediatR-style API and a minimal dependency footprint.

Previously published as **Pipelink**. Orient.MediatR is an independent project, not affiliated with MediatR or Lucky Penny Software.

## Features

- Request/response pattern (`IRequest<T>` / `IRequestHandler<,>`)
- Notification publishing to multiple handlers
- Streaming requests via `IAsyncEnumerable<T>`
- Opt-in pipeline behaviors for cross-cutting concerns
- Automatic handler registration through assembly scanning
- Handlers resolved per dispatch — transient and scoped lifetimes (e.g. EF Core `DbContext`) work correctly

## Installation

```bash
dotnet add package Orient.MediatR
```

Requires .NET 10 (`net10.0`).

## Quick Start

```csharp
using Orient.MediatR.Extensions;
using Orient.MediatR.Handlers;
using Orient.MediatR.Interfaces;

// Register (Program.cs)
builder.Services.AddOrientMediatR(cfg =>
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

// Inject IMediator anywhere and send
var user = await mediator.Send(new GetUserQuery(1));
```

Pipeline behaviors are opt-in and run in registration order:

```csharp
builder.Services.AddOrientMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

## Migrating from Pipelink

Replace `using Pipelink.` with `using Orient.MediatR.`, `AddPipelink` with `AddOrientMediatR` and `IPipelink` with `IMediator`. The old `AddPipelink` and `IPipelink` names still compile as `[Obsolete]` bridges until the next major version.

## Documentation

For full documentation, the MediatR and Pipelink migration guides, and samples, visit the [GitHub repository](https://github.com/bburaksseyhan/pipelink).

## License

MIT
