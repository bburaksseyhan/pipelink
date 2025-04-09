# Pipelink

A lightweight implementation for executing command-query responsibility segregation (CQRS) patterns within applications. It acts as a mediator to send requests and publish notifications by delegating tasks to their respective handlers and/or behaviors.

## Features

- Request/Response pattern
- Notification publishing
- Stream requests
- Pipeline behaviors
- Compression support
- Easy integration with dependency injection

## Installation

```bash
dotnet add package Pipelink
```

## Quick Start

```csharp
// Register Pipelink
services.AddPipelink();

// Create a request
public class MyRequest : IRequest<MyResponse>
{
    public string Message { get; set; }
}

// Create a handler
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Task<MyResponse> Handle(MyRequest request)
    {
        return Task.FromResult(new MyResponse { Result = $"Processed: {request.Message}" });
    }
}

// Use Pipelink
var response = await pipelink.Send(new MyRequest { Message = "Hello" });
```

## Documentation

For more information, visit the [GitHub repository](https://github.com/seyhanb/pipelink).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 