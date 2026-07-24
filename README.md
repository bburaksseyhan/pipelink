# Pipelink

[![NuGet](https://img.shields.io/nuget/v/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![License](https://img.shields.io/github/license/bburaksseyhan/pipelink.svg)](LICENSE)

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
    private readonly IPipelink _pipelink;

    public UserController(IPipelink pipelink)
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

// Register the behavior. Behaviors are opt-in and run in registration order.
services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```