using Microsoft.Extensions.DependencyInjection;
using Orient.MediatR.Behaviors;
using Orient.MediatR.Extensions;
using Orient.MediatR.Handlers;
using Orient.MediatR.Interfaces;
using System.Runtime.CompilerServices;

namespace Orient.MediatR.Tests;

public class MediatorTests
{
    private static ServiceProvider BuildProvider(Action<OrientMediatRConfiguration>? configure = null, Action<IServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        services?.Invoke(collection);
        collection.AddOrientMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<MediatorTests>();
            configure?.Invoke(cfg);
        });
        return collection.BuildServiceProvider();
    }

    [Fact]
    public async Task Send_ReturnsHandlerResponse()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("hello"));

        Assert.Equal("echo: hello", response);
    }

    [Fact]
    public async Task Send_ExecutesBehaviorsInRegistrationOrder()
    {
        var log = new List<string>();
        await using var provider = BuildProvider(
            cfg =>
            {
                cfg.AddOpenBehavior(typeof(FirstBehavior<,>));
                cfg.AddOpenBehavior(typeof(SecondBehavior<,>));
            },
            services => services.AddSingleton(log));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new EchoRequest("x"));

        Assert.Equal(new[] { "first:before", "second:before", "second:after", "first:after" }, log);
    }

    [Fact]
    public async Task Send_ResolvesHandlerPerCall_RespectingTransientLifetime()
    {
        CountingHandler.Instances = 0;
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new CountingRequest());
        await mediator.Send(new CountingRequest());
        await mediator.Send(new CountingRequest());

        Assert.Equal(3, CountingHandler.Instances);
    }

    [Fact]
    public async Task Send_SupportsScopedDependenciesInHandlers()
    {
        await using var provider = BuildProvider(services: s => s.AddScoped<ScopedDependency>());

        string firstScopeId;
        string secondScopeId;

        using (var scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            firstScopeId = await mediator.Send(new ScopedRequest());
        }

        using (var scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            secondScopeId = await mediator.Send(new ScopedRequest());
        }

        Assert.NotEqual(firstScopeId, secondScopeId);
    }

    [Fact]
    public async Task Publish_InvokesAllHandlers()
    {
        var log = new List<string>();
        await using var provider = BuildProvider(services: s => s.AddSingleton(log));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new TestNotification(7));

        Assert.Contains("handler-a:7", log);
        Assert.Contains("handler-b:7", log);
    }

    [Fact]
    public async Task SendStream_YieldsAllItems()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var items = new List<int>();
        await foreach (var item in mediator.SendStream(new NumbersRequest(5)))
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, items);
    }

    [Fact]
    public async Task Send_ThrowsWhenNoHandlerRegistered()
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        // Scan an assembly without handlers for this request type
        collection.AddOrientMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(string).Assembly));
        await using var provider = collection.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new EchoRequest("x")));
    }

#pragma warning disable CS0618
    [Fact]
    public async Task AddPipelink_ShouldRegisterMediatorAndPipelinkBridge_WhenUsedByPipelinkCode()
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatorTests>());
        await using var provider = collection.BuildServiceProvider();

        var legacy = provider.GetRequiredService<IPipelink>();
        var response = await legacy.Send(new EchoRequest("legacy"));

        Assert.Equal("echo: legacy", response);
        Assert.IsType<Implementation.Mediator>(legacy);
        Assert.IsType<Implementation.Mediator>(provider.GetRequiredService<IMediator>());
    }
#pragma warning restore CS0618
}

// --- Test fixtures ---

public record EchoRequest(string Message) : IRequest<string>;

public class EchoRequestHandler : IRequestHandler<EchoRequest, string>
{
    public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"echo: {request.Message}");
}

public record CountingRequest : IRequest<int>;

public class CountingHandler : IRequestHandler<CountingRequest, int>
{
    public static int Instances;

    public CountingHandler() => Interlocked.Increment(ref Instances);

    public Task<int> Handle(CountingRequest request, CancellationToken cancellationToken)
        => Task.FromResult(Instances);
}

public class ScopedDependency
{
    public string Id { get; } = Guid.NewGuid().ToString();
}

public record ScopedRequest : IRequest<string>;

public class ScopedRequestHandler : IRequestHandler<ScopedRequest, string>
{
    private readonly ScopedDependency _dependency;

    public ScopedRequestHandler(ScopedDependency dependency) => _dependency = dependency;

    public Task<string> Handle(ScopedRequest request, CancellationToken cancellationToken)
        => Task.FromResult(_dependency.Id);
}

public record TestNotification(int Value) : INotification;

public class NotificationHandlerA : INotificationHandler<TestNotification>
{
    private readonly List<string> _log;
    public NotificationHandlerA(List<string> log) => _log = log;

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"handler-a:{notification.Value}");
        return Task.CompletedTask;
    }
}

public class NotificationHandlerB : INotificationHandler<TestNotification>
{
    private readonly List<string> _log;
    public NotificationHandlerB(List<string> log) => _log = log;

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"handler-b:{notification.Value}");
        return Task.CompletedTask;
    }
}

public record NumbersRequest(int Count) : IStreamRequest<int>;

public class NumbersRequestHandler : IStreamRequestHandler<NumbersRequest, int>
{
    public async IAsyncEnumerable<int> Handle(NumbersRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}

public class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    public FirstBehavior(List<string> log) => _log = log;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        _log.Add("first:before");
        var response = await next();
        _log.Add("first:after");
        return response;
    }
}

public class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _log;
    public SecondBehavior(List<string> log) => _log = log;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        _log.Add("second:before");
        var response = await next();
        _log.Add("second:after");
        return response;
    }
}
