using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;
using Pipelink.Implementation;

namespace Pipelink.Tests;

public class PipelinkSendTests
{
    [Fact]
    public async Task Send_ShouldReturnHandlerResponse_WhenHandlerRegistered()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<PipelinkSendTests>());
        services.AddTransient<IRequestHandler<PingRequest, string>, PingHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();

        var result = await mediator.Send(new PingRequest("hello"));

        Assert.Equal("pong:hello", result);
    }

    [Fact]
    public async Task Send_ShouldRunPipelineBehaviorsInRegistrationOrder_WhenBehaviorsRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Pipelink.Implementation.Pipelink>();
        services.AddSingleton<OrderTracker>();
        services.AddTransient<IRequestHandler<OrderProbeRequest, string>, OrderProbeHandler>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();
        var tracker = provider.GetRequiredService<OrderTracker>();

        var result = await mediator.Send(new OrderProbeRequest());

        Assert.Equal("handler", result);
        Assert.Equal(["First", "Second", "handler"], tracker.Steps);
    }

    [Fact]
    public async Task Send_ShouldInvokeDefaultBehaviors_WhenUsingAddPipelinkScan()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<PipelinkSendTests>());
        services.AddTransient<IRequestHandler<NullGuardRequest, string>, NullGuardHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send((NullGuardRequest)null!));
    }

    [Fact]
    public async Task Send_ShouldResolveTransientBehaviorPerCall_WhenBehaviorRegisteredAsTransient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Pipelink.Implementation.Pipelink>();
        services.AddSingleton<OrderTracker>();
        services.AddTransient<IRequestHandler<OrderProbeRequest, string>, OrderProbeHandler>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(InstanceCountingBehavior<,>));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();
        var tracker = provider.GetRequiredService<OrderTracker>();

        await mediator.Send(new OrderProbeRequest());
        await mediator.Send(new OrderProbeRequest());

        Assert.Equal(2, tracker.Steps.Count(s => s.StartsWith("instance:")));
        Assert.Equal(2, tracker.Steps.Where(s => s.StartsWith("instance:")).Distinct().Count());
    }

    [Fact]
    public async Task Send_ShouldDispatchByResponseType_WhenRequestImplementsMultipleResponses()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Pipelink.Implementation.Pipelink>();
        services.AddTransient<IRequestHandler<DualRequest, string>, DualHandler>();
        services.AddTransient<IRequestHandler<DualRequest, int>, DualHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();
        var request = new DualRequest();

        Assert.Equal("text", await mediator.Send<string>(request));
        Assert.Equal(42, await mediator.Send<int>(request));
    }

    public record PingRequest(string Message) : IRequest<string>;

    public record DualRequest : IRequest<string>, IRequest<int>;

    private sealed class DualHandler : IRequestHandler<DualRequest, string>, IRequestHandler<DualRequest, int>
    {
        Task<string> IRequestHandler<DualRequest, string>.Handle(DualRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("text");

        Task<int> IRequestHandler<DualRequest, int>.Handle(DualRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(42);
    }

    private sealed class InstanceCountingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly OrderTracker _tracker;
        private readonly Guid _id = Guid.NewGuid();

        public InstanceCountingBehavior(OrderTracker tracker) => _tracker = tracker;

        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _tracker.Steps.Add($"instance:{_id}");
            return next();
        }
    }

    private sealed class PingHandler : IRequestHandler<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, CancellationToken cancellationToken) =>
            Task.FromResult($"pong:{request.Message}");
    }

    public record OrderProbeRequest : IRequest<string>;

    private sealed class OrderProbeHandler : IRequestHandler<OrderProbeRequest, string>
    {
        private readonly OrderTracker _tracker;

        public OrderProbeHandler(OrderTracker tracker) => _tracker = tracker;

        public Task<string> Handle(OrderProbeRequest request, CancellationToken cancellationToken)
        {
            _tracker.Steps.Add("handler");
            return Task.FromResult("handler");
        }
    }

    private sealed class OrderTracker
    {
        public List<string> Steps { get; } = [];
    }

    private sealed class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly OrderTracker _tracker;

        public FirstBehavior(OrderTracker tracker) => _tracker = tracker;

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _tracker.Steps.Add("First");
            return await next();
        }
    }

    private sealed class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly OrderTracker _tracker;

        public SecondBehavior(OrderTracker tracker) => _tracker = tracker;

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _tracker.Steps.Add("Second");
            return await next();
        }
    }

    public record NullGuardRequest : IRequest<string>;

    private sealed class NullGuardHandler : IRequestHandler<NullGuardRequest, string>
    {
        public Task<string> Handle(NullGuardRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("ok");
    }
}
