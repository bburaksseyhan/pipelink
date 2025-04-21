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

```