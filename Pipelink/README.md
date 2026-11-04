# Pipelink

A lightweight, MIT-licensed mediator for CQRS on .NET 10. Send requests, publish notifications and stream responses through their handlers, with an optional pipeline of behaviors.

## Features

- Request/response (`Send`) with `IPipelineBehavior` pipeline
- Notification publishing (`Publish`)
- Stream requests (`SendStream`)
- Assembly scanning for handlers via `AddPipelink`
- Works in ASP.NET Core, Worker Service and console hosts

## Requirements

.NET 10 (`net10.0`). Version 2.x drops .NET 8/9; use Pipelink 1.x for older runtimes.

## Installation

```bash
dotnet add package Pipelink
```

## Quick Start

```csharp
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;

builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

public record GetUserQuery(int Id) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new UserDto(request.Id, "John Doe"));
}

app.MapGet("/user/{id}", (int id, Pipelink.Implementation.Pipelink pipelink) =>
    pipelink.Send(new GetUserQuery(id)));
```

`Pipelink` is registered as transient so handlers resolve from the current DI scope. Inject it where you use it instead of resolving it once from `app.Services`.

## Upgrading from 1.x

- Target framework is `net10.0` only.
- Registered `IPipelineBehavior`s now run on `Send`.
- `Pipelink` is transient instead of singleton.

See the [CHANGELOG](https://github.com/bburaksseyhan/pipelink/blob/production/CHANGELOG.md) for details.

## Documentation

[GitHub repository](https://github.com/bburaksseyhan/pipelink)

## License

MIT
