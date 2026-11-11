# Orient.MediatR

<img src="assets/icon.png" alt="Orient" width="96" />

[![NuGet](https://img.shields.io/nuget/v/Orient.MediatR.svg)](https://www.nuget.org/packages/Orient.MediatR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Orient.MediatR.svg)](https://www.nuget.org/packages/Orient.MediatR)
[![CI](https://github.com/bburaksseyhan/pipelink/actions/workflows/ci.yml/badge.svg)](https://github.com/bburaksseyhan/pipelink/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/bburaksseyhan/pipelink.svg)](LICENSE)

A lightweight, MIT-licensed mediator for .NET and part of the Orient family. Orient.MediatR implements the request/response, notification, and streaming patterns commonly used with CQRS, and keeps its dependency footprint minimal: no ASP.NET Core reference, no third-party dependencies at all — only `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions`.

Orient.MediatR was previously published as [Pipelink](https://www.nuget.org/packages/Pipelink). See [Migrating from Pipelink](#migrating-from-pipelink).

Orient.MediatR is an independent project. It is not affiliated with or endorsed by [MediatR](https://github.com/jbogard/MediatR) or Lucky Penny Software.

## Why Orient.MediatR?

Orient.MediatR is inspired by [MediatR](https://github.com/jbogard/MediatR). If you are looking for a free, lightweight alternative with a familiar API, it offers:

- **Free and MIT-licensed** — no commercial licensing
- **Familiar API** — `IMediator`, `IRequest<T>`, `IRequestHandler<,>`, `INotification`, pipeline behaviors; migration from MediatR takes minutes
- **Built-in streaming** — `IStreamRequest<T>` / `IAsyncEnumerable<T>` support in the core
- **Correct DI lifetimes** — handlers are resolved from the container on every dispatch, so transient and scoped dependencies (such as EF Core `DbContext`) work exactly as registered
- **Fast dispatch** — strongly-typed dispatchers are compiled once per request type and cached; no per-call reflection
- **Opt-in behaviors** — nothing runs in your pipeline unless you register it

## Installation

```bash
dotnet add package Orient.MediatR
```

Requires .NET 10 (`net10.0`).

## Quick Start

### 1. Register Orient.MediatR

```csharp
// Program.cs
using Orient.MediatR.Extensions;

builder.Services.AddOrientMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});
```

Handlers are discovered and registered automatically via assembly scanning.

### 2. Create a request and a handler

```csharp
using Orient.MediatR.Handlers;
using Orient.MediatR.Interfaces;

public record GetUserQuery(int UserId) : IRequest<UserDto>;

public record UserDto(int Id, string Name, string Email);

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _users; // scoped dependencies are fully supported

    public GetUserQueryHandler(IUserRepository users) => _users = users;

    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => _users.FindByIdAsync(request.UserId, cancellationToken);
}
```

### 3. Send the request

```csharp
// Minimal API
app.MapGet("/user/{id}", async (int id, IMediator mediator, CancellationToken ct) =>
{
    var user = await mediator.Send(new GetUserQuery(id), ct);
    return Results.Ok(user);
});

// Or inject IMediator into any controller or service
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
        => Ok(await _mediator.Send(new GetUserQuery(id)));
}
```

## Notifications

Publish an event to any number of handlers. Handlers run sequentially in registration order.

```csharp
public record UserCreatedNotification(int UserId, string Email) : INotification;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        // send welcome email
        return Task.CompletedTask;
    }
}

public class InvalidateUserCacheHandler : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken ct)
    {
        // invalidate cache
        return Task.CompletedTask;
    }
}

await _mediator.Publish(new UserCreatedNotification(userId, email));
```

## Streaming

Stream results as they are produced, with full cancellation support.

```csharp
public record StreamUsersQuery(int Count) : IStreamRequest<UserDto>;

public class StreamUsersQueryHandler : IStreamRequestHandler<StreamUsersQuery, UserDto>
{
    public async IAsyncEnumerable<UserDto> Handle(
        StreamUsersQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            var user = await FetchNextUserAsync(i, cancellationToken);
            yield return user;
        }
    }
}

await foreach (var user in _mediator.SendStream(new StreamUsersQuery(100), ct))
{
    Console.WriteLine(user.Name);
}
```

## Pipeline Behaviors

Behaviors wrap request handling with cross-cutting concerns (logging, validation, caching, transactions). They are **opt-in** and run in **registration order** — the first registered behavior is the outermost.

```csharp
public class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TimingBehavior<TRequest, TResponse>> _logger;

    public TimingBehavior(ILogger<TimingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        _logger.LogInformation("{Request} handled in {Elapsed} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        return response;
    }
}

builder.Services.AddOrientMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(TimingBehavior<,>));   // outermost
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));  // runs inside TimingBehavior
});
```

Streaming requests have their own behavior contract: implement `IStreamPipelineBehavior<TRequest, TResponse>` and register it the same way.

### Built-in behaviors

Orient.MediatR ships two ready-made behaviors you can opt into:

- `LoggingBehavior<,>` — logs the start and completion of every request via `ILogger`
- `CachingBehavior<,>` — caches responses in memory, keyed by the request. Use `record` requests (value equality) for cache hits to work, and avoid it for responses containing per-user or sensitive data. The cache is process-wide and unbounded.

## Migrating from Pipelink

Orient.MediatR 1.0.0 is Pipelink 2.0.0 under a new package ID, namespace and entry point. Behavior and performance are unchanged.

1. Swap the package:

   ```bash
   dotnet remove package Pipelink
   dotnet add package Orient.MediatR
   ```

2. Replace the namespaces: `using Pipelink.` becomes `using Orient.MediatR.` (`Interfaces`, `Handlers`, `Behaviors`, `Extensions`, `Implementation` keep their names).

3. Rename the two entry points. The old names still compile as `[Obsolete]` bridges, so you can do this step later:

| Pipelink 2.x | Orient.MediatR 1.x |
|---|---|
| `services.AddPipelink(cfg => ...)` | `services.AddOrientMediatR(cfg => ...)` |
| `IPipelink` | `IMediator` (`IPipelink` still resolves from DI, to the same mediator) |
| `Pipelink.Implementation.Pipelink` | `Orient.MediatR.Implementation.Mediator` |
| `PipelinkConfiguration` | `OrientMediatRConfiguration` |
| `AddPipelinkHandlersAndBehaviors()` | removed; use `AddOrientMediatR()` |

The `IPipelink` and `AddPipelink` bridges will be removed in the next major version.

## Migrating from MediatR

The concepts map one-to-one; most migrations are a find-and-replace:

| MediatR | Orient.MediatR |
|---------|----------|
| `IMediator` / `ISender` | `IMediator` |
| `services.AddMediatR(cfg => ...)` | `services.AddOrientMediatR(cfg => ...)` |
| `cfg.RegisterServicesFromAssemblyContaining<T>()` | same |
| `IRequest<TResponse>` | same |
| `IRequestHandler<TRequest, TResponse>` | same |
| `INotification` / `INotificationHandler<T>` | same |
| `IStreamRequest<T>` / `CreateStream(...)` | `IStreamRequest<T>` / `SendStream(...)` |
| `IPipelineBehavior<TRequest, TResponse>` | same, but note the signature below |
| `cfg.AddOpenBehavior(typeof(...))` | same |

The one signature difference — in Orient.MediatR, `next` is the **last** parameter of a behavior's `Handle`:

```csharp
// MediatR
Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);

// Orient.MediatR
Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
```

Other differences to be aware of:

- Behaviors are not discovered by assembly scanning; register each with `cfg.AddOpenBehavior(...)`
- `Publish` invokes handlers sequentially and stops at the first exception (no publisher strategies yet)
- If a project references both packages during a migration, `IMediator`, `IRequest<T>` and the other contracts exist in both the `MediatR` and `Orient.MediatR.*` namespaces. Use aliases (`using LuckyPennyMediator = global::MediatR.IMediator;`) or fully qualified names. Inside an `Orient.*` namespace, write `global::MediatR.` because `MediatR.` resolves to `Orient.MediatR`.

## Performance

Orient.MediatR compiles a strongly-typed dispatcher per request type on first use and caches it, avoiding per-call reflection. When no behaviors are registered for a request, the handler is called directly.

`Send` with one handler and no behaviors, measured with BenchmarkDotNet 0.14.0 on .NET 10.0.2 (Apple M3, 15 iterations, as Pipelink 2.0.0 — the dispatch code is unchanged):

| Library | Mean | Allocated |
|---|---:|---:|
| MediatR 12.5.0 | 67.2 ns | 272 B |
| Orient.MediatR 1.0.0 | 53.5 ns | 208 B |

Run the included [BenchmarkDotNet suite](benchmarks/Orient.MediatR.Benchmarks) to measure on your own hardware:

```bash
dotnet run --project benchmarks/Orient.MediatR.Benchmarks -c Release
```

## Repository Layout

- [`src/Orient.MediatR`](src/Orient.MediatR) — the library (published to NuGet)
- [`tests/Orient.MediatR.Tests`](tests/Orient.MediatR.Tests) — xUnit unit tests
- [`tests/Orient.MediatR.IntegrationTests`](tests/Orient.MediatR.IntegrationTests) — the samples exercised over HTTP, on a real generic host and as separate processes
- [`samples/Orient.MediatR.SampleApi`](samples/Orient.MediatR.SampleApi) — ASP.NET Core minimal API showing `Send`, `SendStream`, `Publish`, and behaviors
- [`samples/Orient.MediatR.SampleWorker`](samples/Orient.MediatR.SampleWorker) — Worker Service sending a command per tick in its own DI scope (`--run-once` to exit after one tick)
- [`samples/Orient.MediatR.SampleConsole`](samples/Orient.MediatR.SampleConsole) — console app on the generic host
- [`benchmarks/Orient.MediatR.Benchmarks`](benchmarks/Orient.MediatR.Benchmarks) — BenchmarkDotNet comparisons
- [`build`](build) — `validate-packages.sh` checks the packed `.nupkg`; `verify-consumer.sh` installs it into fresh Console, Worker and Web API projects and runs them

## Building a package locally

```bash
dotnet pack src/Orient.MediatR -c Release -o artifacts/packages
bash build/validate-packages.sh artifacts/packages
bash build/verify-consumer.sh artifacts/packages
```

## Contributing

Issues and pull requests are welcome. Please make sure `dotnet test Orient.MediatR.slnx` passes before submitting.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
