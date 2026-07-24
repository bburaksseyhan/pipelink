# Pipelink

[![NuGet](https://img.shields.io/nuget/v/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![CI](https://github.com/bburaksseyhan/pipelink/actions/workflows/ci.yml/badge.svg)](https://github.com/bburaksseyhan/pipelink/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/bburaksseyhan/pipelink.svg)](LICENSE)

A lightweight, MIT-licensed mediator for .NET. Pipelink implements the request/response, notification, and streaming patterns commonly used with CQRS, and keeps its dependency footprint minimal: no ASP.NET Core reference, no third-party dependencies at all — only `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions`.

## Why Pipelink?

Pipelink is inspired by [MediatR](https://github.com/jbogard/MediatR). If you are looking for a free, lightweight alternative with a familiar API, Pipelink offers:

- **Free and MIT-licensed** — no commercial licensing
- **Familiar API** — `IRequest<T>`, `IRequestHandler<,>`, `INotification`, pipeline behaviors; migration from MediatR takes minutes
- **Built-in streaming** — `IStreamRequest<T>` / `IAsyncEnumerable<T>` support in the core
- **Correct DI lifetimes** — handlers are resolved from the container on every dispatch, so transient and scoped dependencies (such as EF Core `DbContext`) work exactly as registered
- **Fast dispatch** — strongly-typed dispatchers are compiled once per request type and cached; no per-call reflection
- **Opt-in behaviors** — nothing runs in your pipeline unless you register it

## Installation

```bash
dotnet add package Pipelink
```

Requires .NET 8.0 or later.

## Quick Start

### 1. Register Pipelink

```csharp
// Program.cs
builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});
```

Handlers are discovered and registered automatically via assembly scanning.

### 2. Create a request and a handler

```csharp
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
app.MapGet("/user/{id}", async (int id, IPipelink pipelink, CancellationToken ct) =>
{
    var user = await pipelink.Send(new GetUserQuery(id), ct);
    return Results.Ok(user);
});

// Or inject IPipelink into any controller or service
public class UserController : ControllerBase
{
    private readonly IPipelink _pipelink;

    public UserController(IPipelink pipelink) => _pipelink = pipelink;

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
        => Ok(await _pipelink.Send(new GetUserQuery(id)));
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

await _pipelink.Publish(new UserCreatedNotification(userId, email));
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

await foreach (var user in _pipelink.SendStream(new StreamUsersQuery(100), ct))
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

builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(TimingBehavior<,>));   // outermost
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));  // runs inside TimingBehavior
});
```

Streaming requests have their own behavior contract: implement `IStreamPipelineBehavior<TRequest, TResponse>` and register it the same way.

### Built-in behaviors

Pipelink ships two ready-made behaviors you can opt into:

- `LoggingBehavior<,>` — logs the start and completion of every request via `ILogger`
- `CachingBehavior<,>` — caches responses in memory, keyed by the request. Use `record` requests (value equality) for cache hits to work, and avoid it for responses containing per-user or sensitive data. The cache is process-wide and unbounded.

## Migrating from MediatR

The concepts map one-to-one; most migrations are a find-and-replace:

| MediatR | Pipelink |
|---------|----------|
| `IMediator` / `ISender` | `IPipelink` |
| `services.AddMediatR(cfg => ...)` | `services.AddPipelink(cfg => ...)` |
| `cfg.RegisterServicesFromAssemblyContaining<T>()` | same |
| `IRequest<TResponse>` | same |
| `IRequestHandler<TRequest, TResponse>` | same |
| `INotification` / `INotificationHandler<T>` | same |
| `IStreamRequest<T>` / `CreateStream(...)` | `IStreamRequest<T>` / `SendStream(...)` |
| `IPipelineBehavior<TRequest, TResponse>` | same, but note the signature below |
| `cfg.AddOpenBehavior(typeof(...))` | same |

The one signature difference — in Pipelink, `next` is the **last** parameter of a behavior's `Handle`:

```csharp
// MediatR
Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);

// Pipelink
Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
```

Other differences to be aware of:

- Behaviors are not discovered by assembly scanning; register each with `cfg.AddOpenBehavior(...)`
- `Publish` invokes handlers sequentially and stops at the first exception (no publisher strategies yet)

## Performance

Pipelink compiles a strongly-typed dispatcher per request type on first use and caches it, avoiding per-call reflection. Earlier benchmarks (v1.0.x) measured `Send` at roughly half the latency and 27% fewer allocations than MediatR on the same machine; those numbers predate the behavior pipeline introduced in v1.1.0 and are being re-measured. Run the included [BenchmarkDotNet suite](Pipelink.Benchmarks) to measure on your own hardware:

```bash
dotnet run --project Pipelink.Benchmarks -c Release
```

## Repository Layout

- [`Pipelink`](Pipelink) — the library (published to NuGet)
- [`Pipelink.Tests`](Pipelink.Tests) — xUnit test suite
- [`PipelinkTest.Api`](PipelinkTest.Api) — sample ASP.NET Core minimal API showing `Send`, `SendStream`, `Publish`, and behaviors
- [`Pipelink.Benchmarks`](Pipelink.Benchmarks) — BenchmarkDotNet comparisons

## Contributing

Issues and pull requests are welcome. Please make sure `dotnet test` passes before submitting.

## License

This project is licensed under the MIT License — see the [LICENSE](Pipelink/LICENSE) file for details.
