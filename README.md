# Pipelink

Pipelink is a lightweight, high-performance mediator pattern implementation for .NET applications, inspired by MediatR. It provides a simple way to implement the mediator pattern in your applications, allowing for clean separation of concerns and better maintainability.

## Features

- **Request/Response Pattern**: Send requests and receive responses through a mediator
- **Notification Pattern**: Publish notifications to multiple handlers
- **Pipeline Behaviors**: Add cross-cutting concerns like validation, logging, and caching
- **Dependency Injection**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Assembly Scanning**: Automatic registration of handlers and behaviors
- **Async Support**: Full support for asynchronous operations
- **Type Safety**: Strongly typed requests, responses, and notifications

## Installation

```bash
dotnet add package Pipelink
```

## Quick Start

1. Register Pipelink in your application:

```csharp
services.AddPipelink(cfg => 
{
    cfg.RegisterServicesFromAssemblyContaining<Startup>();
});
```

2. Create a request:

```csharp
public record GetUserQuery(int UserId) : IRequest<UserDto>;
```

3. Create a handler:

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Your implementation here
        return new UserDto { /* ... */ };
    }
}
```

4. Use the mediator in your application:

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
public record UserCreatedNotification(int UserId) : INotification;

// Create a handler
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send welcome email
    }
}

// Publish a notification
await _mediator.Publish(new UserCreatedNotification(userId));
```

### Pipeline Behaviors

```csharp
// Create a behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Log before
        var response = await next();
        // Log after
        return response;
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

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by [MediatR](https://github.com/jbogard/MediatR)
- Built with ❤️ for the .NET community