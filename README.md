# Pipelink

[![NuGet](https://img.shields.io/nuget/v/Pipelink.svg)](https://www.nuget.org/packages/Pipelink)
[![License](https://img.shields.io/github/license/seyhanb/pipelink.svg)](LICENSE)

Pipelink is a lightweight, high-performance mediator pattern implementation for .NET applications, inspired by MediatR. It provides a simple way to implement the mediator pattern in your applications, allowing for clean separation of concerns and better maintainability.

## Features

- **Request/Response Pattern**: Send requests and receive responses through a mediator
- **Notification Pattern**: Publish notifications to multiple handlers
- **Pipeline Behaviors**: Add cross-cutting concerns like validation, logging, and caching
- **Dependency Injection**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Assembly Scanning**: Automatic registration of handlers and behaviors
- **Async Support**: Full support for asynchronous operations
- **Type Safety**: Strongly typed requests, responses, and notifications
- **High Performance**: Optimized for minimal overhead and memory usage

## Performance

Pipelink is designed with performance in mind. Here are some key benchmarks:

| Operation | Mean Time | Allocated Memory |
|-----------|-----------|------------------|
| Send Request | ~1-2μs | ~200B |
| Publish Notification | ~0.5-1μs | ~150B |
| Send with Pipeline Behavior | ~2-3μs | ~300B |

*Note: Benchmarks were run on .NET 8.0, Release configuration. Your results may vary depending on hardware and environment.*

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

### 4. Use the mediator in your application

```csharp
public class UserController : ControllerBase
{
    private readonly Pipelink _mediator;

    public UserController(Pipelink mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _mediator.Send(new GetUserQuery(id));
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
await _mediator.Publish(new UserCreatedNotification(userId, email));
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

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

## Configuration

Pipelink can be configured using the `PipelinkConfiguration` class:

```csharp
services.AddPipelink(cfg => 
{
    // Register from multiple assemblies
    cfg.RegisterServicesFromAssemblyContaining<Startup>()
       .RegisterServicesFromAssembly(typeof(SomeOtherClass).Assembly);
});
```

## Best Practices

1. **Keep Handlers Focused**: Each handler should have a single responsibility
2. **Use Records for Requests**: Records are immutable and perfect for request/response objects
3. **Implement Validation**: Use pipeline behaviors for request validation
4. **Handle Errors**: Implement error handling behaviors
5. **Use Cancellation Tokens**: Always pass cancellation tokens through your handlers
6. **Log Important Events**: Use logging behaviors for important operations

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository
2. Install .NET 8.0 SDK
3. Run tests: `dotnet test`
4. Run benchmarks: `dotnet run -c Release --project Pipelink.Benchmarks`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by [MediatR](https://github.com/jbogard/MediatR)
- Built with ❤️ for the .NET community