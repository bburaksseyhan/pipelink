using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Pipelink.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5, launchCount: 1)]
public class MediatorBenchmarks
{
    private IMediator _mediatR = null!;
    private Implementation.Pipelink _pipelinkMinimal = null!;
    private Implementation.Pipelink _pipelinkWithBehaviors = null!;
    private TestRequest _request = null!;
    private TestNotification _notification = null!;
    private TestStreamRequest _streamRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        _mediatR = mediatRServices.BuildServiceProvider().GetRequiredService<IMediator>();

        var pipelinkMinimalServices = new ServiceCollection();
        pipelinkMinimalServices.AddSingleton<Implementation.Pipelink>();
        pipelinkMinimalServices.AddTransient<Pipelink.Handlers.IRequestHandler<TestRequest, string>, TestRequestHandler>();
        pipelinkMinimalServices.AddTransient<Pipelink.Handlers.INotificationHandler<TestNotification>, TestNotificationHandler>();
        pipelinkMinimalServices.AddTransient<Pipelink.Handlers.IStreamRequestHandler<TestStreamRequest, int>, TestStreamRequestHandler>();
        _pipelinkMinimal = pipelinkMinimalServices.BuildServiceProvider().GetRequiredService<Implementation.Pipelink>();

        var pipelinkPipelineServices = new ServiceCollection();
        pipelinkPipelineServices.AddSingleton<Implementation.Pipelink>();
        pipelinkPipelineServices.AddTransient<Pipelink.Handlers.IRequestHandler<TestRequest, string>, TestRequestHandler>();
        pipelinkPipelineServices.AddTransient(typeof(Pipelink.Behaviors.IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        pipelinkPipelineServices.AddTransient(typeof(Pipelink.Behaviors.IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        pipelinkPipelineServices.AddTransient(typeof(Pipelink.Behaviors.IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        _pipelinkWithBehaviors = pipelinkPipelineServices.BuildServiceProvider().GetRequiredService<Implementation.Pipelink>();

        _request = new TestRequest { Message = "Test" };
        _notification = new TestNotification(1);
        _streamRequest = new TestStreamRequest(8);
    }

    [Benchmark(Baseline = true)]
    public async Task<string> MediatR_Send() => await _mediatR.Send(_request);

    [Benchmark]
    public async Task<string> Pipelink_Send() => await _pipelinkMinimal.Send(_request);

    [Benchmark]
    public async Task<string> Pipelink_SendWithBehaviors() => await _pipelinkWithBehaviors.Send(_request);

    [Benchmark]
    public async Task MediatR_Publish() => await _mediatR.Publish(_notification);

    [Benchmark]
    public async Task Pipelink_Publish() => await _pipelinkMinimal.Publish(_notification);

    [Benchmark]
    public async Task MediatR_SendStream()
    {
        await foreach (var _ in _mediatR.CreateStream(_streamRequest))
        {
        }
    }

    [Benchmark]
    public async Task Pipelink_SendStream()
    {
        await foreach (var _ in _pipelinkMinimal.SendStream(_streamRequest))
        {
        }
    }
}

public class TestRequest : IRequest<string>, Pipelink.Interfaces.IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestHandler :
    MediatR.IRequestHandler<TestRequest, string>,
    Pipelink.Handlers.IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Message);
}

public record TestNotification(int Id) : INotification, Pipelink.Interfaces.INotification;

public class TestNotificationHandler :
    MediatR.INotificationHandler<TestNotification>,
    Pipelink.Handlers.INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public record TestStreamRequest(int Count) : IStreamRequest<int>, Pipelink.Interfaces.IStreamRequest<int>;

public class TestStreamRequestHandler :
    MediatR.IStreamRequestHandler<TestStreamRequest, int>,
    Pipelink.Handlers.IStreamRequestHandler<TestStreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        TestStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
