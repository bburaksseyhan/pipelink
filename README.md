# Pipelink

[![NuGet](https://img.shields.io/nuget/v/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![License](https://img.shields.io/github/license/seyhanb/pipelink.svg)](LICENSE)

A lightweight implementation for executing command-query responsibility segregation (CQRS) patterns within applications. It acts as a mediator to send requests and publish notifications by delegating tasks to their respective handlers and/or behaviors.

## Inspiration

Pipelink is inspired by [MediatR](https://github.com/jbogard/MediatR), a popular .NET library implementing the mediator pattern. While sharing similar core concepts with MediatR, Pipelink offers:

- **Lightweight Implementation**: A streamlined approach focusing on essential CQRS functionality
- **Modern .NET Features**: Built specifically for .NET 8.0, leveraging the latest framework capabilities
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
- **Compression Support**: Built-in support for data compression

## Performance

Pipelink is designed with performance in mind. Here are the latest benchmark results comparing Pipelink with MediatR:

| Method        | Mean     | Error    | StdDev   | Allocated | 
|--------------|----------|----------|----------|-----------|
| MediatR_Send | 83.13 ns | 0.333 ns | 0.642 ns | 384 B    |
| Pipelink_Send| 39.10 ns | 0.179 ns | 0.340 ns | 280 B    |

Key Performance Improvements:
- **53% Faster**: Pipelink is more than twice as fast as MediatR
- **27% Less Memory**: Pipelink allocates significantly less memory per operation
- **More Stable**: Lower standard deviation indicates more consistent performance

*Note: Benchmarks were run on .NET 8.0, Release configuration on an Apple M3 processor using BenchmarkDotNet v0.14.0. Your results may vary depending on hardware and environment.*

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
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
    var startMemory = Process.GetCurrentProcess().WorkingSet64;

    try
    {
        await next();
    }
    finally
    {
        var endTime = DateTime.UtcNow;
        var endCpu = Process.GetCurrentProcess().TotalProcessorTime;
        var endMemory = Process.GetCurrentProcess().WorkingSet64;

        var metrics = new MetricsData
        {
            RequestType = context.Request.Path,
            StartTime = startTime,
            EndTime = endTime,
            DurationMs = (endTime - startTime).TotalMilliseconds,
            CpuUsage = (endCpu - startCpu).TotalMilliseconds,
            MemoryUsage = endMemory - startMemory,
            HasError = context.Response.StatusCode >= 400
        };

        await metricsCollector.RecordMetricsAsync(metrics);
    }
});
```

3. Add the metrics endpoint:
```csharp
app.MapGet("/metrics", async (IMetricsCollector metricsCollector, 
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null,
    [FromQuery] string? requestType = null) =>
{
    var metrics = await metricsCollector.GetMetricsAsync(startTime, endTime, requestType);
    return Results.Ok(metrics);
});
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

#### Querying Metrics

You can retrieve metrics with optional filtering:

```http
GET /metrics                                    # All metrics
GET /metrics?requestType=/user/1                # Metrics for specific endpoint
GET /metrics?startTime=2024-04-21T09:00:00Z    # Metrics after start time
GET /metrics?endTime=2024-04-21T10:00:00Z      # Metrics before end time
```

Example response:
```json
[
    {
        "requestType": "/user/1",
        "startTime": "2024-04-21T09:30:42Z",
        "endTime": "2024-04-21T09:30:42Z",
        "durationMs": 109.32,
        "cpuUsage": 8.44,
        "memoryUsage": 950272,
        "hasError": false,
        "errorMessage": null,
        "cpuPercentage": 7.72,
        "memoryPercentage": 0.45,
        "peakMemoryUsage": 1024000,
        "averageCpuUsage": 0.077
    }
]
```

### Validation Behavior

```