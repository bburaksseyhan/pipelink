using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;

namespace Pipelink.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5, launchCount: 1)]
public class PipelinkBenchmarks
{
    private Implementation.Pipelink _pipelinkMinimal = null!;
    private Implementation.Pipelink _pipelinkWithBehaviors = null!;
    private SimpleRequest _request = null!;
    private SimpleNotification _notification = null!;
    private SimpleStreamRequest _streamRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var minimalServices = new ServiceCollection();
        minimalServices.AddSingleton<Implementation.Pipelink>();
        minimalServices.AddTransient<IRequestHandler<SimpleRequest, SimpleResponse>, SimpleRequestHandler>();
        minimalServices.AddTransient<INotificationHandler<SimpleNotification>, SimpleNotificationHandler>();
        minimalServices.AddTransient<IStreamRequestHandler<SimpleStreamRequest, SimpleStreamResponse>, SimpleStreamRequestHandler>();
        _pipelinkMinimal = minimalServices.BuildServiceProvider().GetRequiredService<Implementation.Pipelink>();

        var pipelineServices = new ServiceCollection();
        pipelineServices.AddSingleton<Implementation.Pipelink>();
        pipelineServices.AddTransient<IRequestHandler<SimpleRequest, SimpleResponse>, SimpleRequestHandler>();
        pipelineServices.AddTransient(typeof(IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        pipelineServices.AddTransient(typeof(IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        pipelineServices.AddTransient(typeof(IPipelineBehavior<,>), typeof(NoOpPipelineBehavior<,>));
        _pipelinkWithBehaviors = pipelineServices.BuildServiceProvider().GetRequiredService<Implementation.Pipelink>();

        _request = new SimpleRequest(1);
        _notification = new SimpleNotification(1);
        _streamRequest = new SimpleStreamRequest(10);
    }

    [Benchmark]
    public async Task Send_SimpleRequest() => await _pipelinkMinimal.Send(_request);

    [Benchmark]
    public async Task Send_RequestWithBehavior() => await _pipelinkWithBehaviors.Send(_request);

    [Benchmark]
    public async Task Publish_SimpleNotification() => await _pipelinkMinimal.Publish(_notification);

    [Benchmark]
    public async Task SendStream_SimpleRequest()
    {
        await foreach (var _ in _pipelinkMinimal.SendStream<SimpleStreamResponse>(_streamRequest))
        {
        }
    }
}
