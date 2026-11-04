# Pipelink

[![NuGet](https://img.shields.io/nuget/v/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![License](https://img.shields.io/github/license/seyhanb/pipelink.svg)](LICENSE)

A lightweight implementation for executing command-query responsibility segregation (CQRS) patterns within applications. It acts as a mediator to send requests and publish notifications by delegating tasks to their respective handlers and/or behaviors.

## Inspiration

Pipelink is inspired by [MediatR](https://github.com/jbogard/MediatR), a popular .NET library implementing the mediator pattern. While sharing similar core concepts with MediatR, Pipelink offers:

- **Lightweight Implementation**: A streamlined approach focusing on essential CQRS functionality
- **Modern .NET Features**: Built for **.NET 10** (LTS), leveraging current framework capabilities
- **Built-in Streaming**: Native support for streaming operations, ideal for handling large datasets
- **Simplified Pipeline**: Straightforward behavior pipeline for cross-cutting concerns
- **Modern Dependency Injection**: Seamless integration with Microsoft's DI container

## Features

- **Request/Response Pattern**: Send requests and receive responses through a mediator
- **Notification Pattern**: Publish notifications to multiple handlers
- **Pipeline Behaviors**: Add cross-cutting concerns like validation, logging, and caching
- **Dependency Injection**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Assembly Scanning**: Automatic registration of handlers and behaviors
- **Async Support**: Full support for asynchronous operations
- **Type Safety**: Strongly typed requests, responses, and notifications
- **High Performance**: Optimized for minimal overhead and memory usage
- **Stream Requests**: Native support for streaming operations
- **Request Metrics (optional)**: ASP.NET Core middleware for in-memory HTTP metrics

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later (see `global.json` for the pinned SDK used by this repo)
- **Breaking change:** Pipelink **2.0.0** targets `net10.0` only. Stay on **1.x** if you must remain on .NET 8/9.

See [CHANGELOG.md](CHANGELOG.md) for migration notes.

## Performance

Benchmarks compare **Pipelink 2.0** (`chore/net10-migration`) with **MediatR 12.5** on the same handlers.

**Environment:** Apple M3, macOS 15.6.1, .NET SDK **10.0.102**, BenchmarkDotNet **0.14.0**, `SimpleJob` (warmup=2, iteration=5, launch=1), filter `*MediatorBenchmarks*`.

| Method | Mean | Allocated | vs MediatR_Send (Mean) |
|--------|-----:|----------:|------------------------:|
| MediatR_Send | 73.6 ns | 272 B | 1.00× (baseline) |
| Pipelink_Send | 36.9 ns | 208 B | **0.52×** |
| Pipelink_SendWithBehaviors | 104.3 ns | 704 B | 1.46× |
| MediatR_Publish | 79.5 ns | 288 B | 1.35× |
| Pipelink_Publish | 34.2 ns | 88 B | **0.58×** |
| MediatR_SendStream | 8,798 ns | 816 B | 149× |
| Pipelink_SendStream | 8,630 ns | 864 B | **146×** |

The three `Send` rows come from a longer run (`--iterationCount 15 --warmupCount 5`); across runs MediatR_Send measured 62–74 ns and Pipelink_Send 37–39 ns. `SendWithBehaviors` uses three transient no-op behaviors, resolved from DI on every call.

**Baseline (.NET 8, `production` worktree):** `Pipelink_Send` ~45.6 ns / 280 B (~0.49× MediatR). **2.0.0+** caches one generic request wrapper per request type: with no behaviors registered, `Send` calls the handler directly with no closures or extra async state machines. Behavior instances are resolved per call so their DI lifetimes are respected.

**Pipelink-only (`PipelinkBenchmarks`):** `Send_SimpleRequest` 318.6 ns; `Send_RequestWithBehavior` 377.6 ns (three no-op behaviors); `Publish` 33.9 ns; `SendStream` (10 items) 542.3 ns.

Reproduce:

```bash
dotnet run -c Release --project Pipelink.Benchmarks -- --filter '*MediatorBenchmarks*'
```

## Installation

```bash
dotnet add package Pipelink
```

## Quick Start

### 1. Register Pipelink in your application

```csharp
// In Program.cs or Startup.cs
services.AddPipelink(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<Startup>();
});
```

### 2. Create a request and response

```csharp
// Request
public record GetUserQuery(int UserId) : IRequest<UserDto>;

// Response
public record UserDto(int Id, string Name, string Email);
```

### 3. Create a handler

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Your implementation here
        return new UserDto(request.UserId, "John Doe", "john@example.com");
    }
}
```

### 4. Use Pipelink in your application

```csharp
public class UserController : ControllerBase
{
    private readonly Pipelink _pipelink;

    public UserController(Pipelink pipelink)
    {
        _pipelink = pipelink;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _pipelink.Send(new GetUserQuery(id));
        return Ok(user);
    }
}
```

## Advanced Usage

### Notifications

```csharp
// Define a notification
public record UserCreatedNotification(int UserId, string Email) : INotification;

// Create multiple handlers
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send welcome email
    }
}

public class UpdateUserCacheHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Update user cache
    }
}

// Publish a notification
await _pipelink.Publish(new UserCreatedNotification(userId, email));
```

### Pipeline Behaviors

```csharp
// Create a behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        _logger.LogInformation("Handling {Request}", request.GetType().Name);
        var response = await next();
        _logger.LogInformation("Handled {Request}", request.GetType().Name);
        return response;
    }
}
```

### Metrics Collection

Pipelink includes built-in support for collecting and monitoring request metrics. This feature helps you track performance, resource usage, and errors across your application.

#### Setting up Metrics Collection

1. Register the metrics collector:
```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();
```

2. Add the metrics middleware:
```csharp
// Add metrics collection middleware
app.UseMetrics();
```

#### Available Metrics Endpoints

The API provides several endpoints for accessing and visualizing metrics:

1. **Get Metrics** - `GET /metrics`
   - Returns raw metrics data
   - Supports filtering by time range and request type
   ```http
   GET /metrics
   GET /metrics?startTime=2024-04-21T09:00:00Z
   GET /metrics?endTime=2024-04-21T10:00:00Z
   GET /metrics?requestType=/user/1
   ```

2. **Export as CSV** - `GET /metrics/export/csv`
   - Exports metrics data in CSV format
   - Easy to import into Excel or other analysis tools
   ```http
   GET /metrics/export/csv
   ```

3. **HTML Visualization** - `GET /metrics/visualize`
   - Provides a clean, formatted HTML table view
   - Includes summary statistics and detailed metrics
   ```http
   GET /metrics/visualize
   ```

4. **Pretty Print JSON** - `GET /metrics/pretty`
   - Returns formatted JSON with human-readable timestamps
   - Rounded numerical values for better readability
   ```http
   GET /metrics/pretty
   ```

5. **Clear Metrics** - `DELETE /metrics`
   - Clears all collected metrics data
   ```http
   DELETE /metrics
   ```

#### Available Metrics

The metrics collector captures the following information for each request:

- **Request Type**: The path of the request (e.g., `/user/1`, `/login`)
- **Timing**:
  - Start Time (UTC)
  - End Time (UTC)
  - Duration (milliseconds)
- **Resource Usage**:
  - CPU Usage (milliseconds)
  - CPU Percentage (%)
  - Average CPU Usage (ms/request)
  - Memory Usage (bytes)
  - Memory Percentage (%)
  - Peak Memory Usage (bytes)
- **Error Tracking**:
  - Has Error flag
  - Error Message (if applicable)

#### Example Metrics Response

```json
[
  {
    "requestType": "/user/1",
    "startTime": "2024-04-21T09:30:42.123Z",
    "endTime": "2024-04-21T09:30:42.234Z",
    "durationMs": 109.32,
    "cpuUsage": 8.44,
    "memoryUsage": 950272,
    "cpuPercentage": 7.72,
    "memoryPercentage": 0.45,
    "peakMemoryUsage": 1024000,
    "hasError": false
  }
]
```

#### Benefits of Metrics Collection

1. **Performance Monitoring**:
   - Track request durations
   - Monitor CPU and memory usage
   - Identify performance bottlenecks

2. **Resource Optimization**:
   - Analyze memory usage patterns
   - Optimize CPU-intensive operations
   - Plan resource allocation

3. **Error Detection**:
   - Monitor error rates
   - Track error patterns
   - Identify problematic endpoints

4. **Capacity Planning**:
   - Understand resource requirements
   - Plan for scaling
   - Optimize infrastructure

#### Best Practices

1. **Regular Monitoring**:
   - Check metrics regularly
   - Set up alerts for anomalies
   - Monitor trends over time

2. **Data Retention**:
   - Clear metrics periodically
   - Archive important data
   - Consider storage limitations

3. **Analysis**:
   - Compare metrics across time periods
   - Identify patterns and trends
   - Use data for optimization

4. **Integration**:
   - Export data for analysis
   - Use visualization tools
   - Create custom dashboards

### Validation Behavior

Pipelink ships with `ValidationBehavior`, which rejects null requests before the handler runs. Extend or replace it with your own `IPipelineBehavior<TRequest, TResponse>` implementations (for example FluentValidation in **your** application layer — the core package does not take a hard dependency on FluentValidation).

Pipeline behaviors registered via `AddPipelink` run **outermost-first** in DI registration order (default: Logging → Validation → Caching → handler).

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        if (request is null)
            throw new ArgumentException("Request cannot be null");

        // Add custom validation (FluentValidation, DataAnnotations, etc.)
        return await next();
    }
}
```

## Building and testing

```bash
dotnet build pipelink.sln -c Release
dotnet test pipelink.sln -c Release
dotnet run --project PipelinkTest.Api
```

Sample **API**, **Worker**, and **Console** hosts: see [docs/samples-hosting.md](docs/samples-hosting.md).