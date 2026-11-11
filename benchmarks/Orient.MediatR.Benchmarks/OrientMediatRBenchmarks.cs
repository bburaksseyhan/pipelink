using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Orient.MediatR.Extensions;
using Orient.MediatR.Implementation;

namespace Orient.MediatR.Benchmarks;

[MemoryDiagnoser]
public class OrientMediatRBenchmarks
{
    private Implementation.Mediator _mediator = null!;
    private SimpleRequest _request = null!;
    private SimpleNotification _notification = null!;
    private SimpleStreamRequest _streamRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddOrientMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblyContaining<OrientMediatRBenchmarks>();
        });

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<Implementation.Mediator>();
        _request = new SimpleRequest(1);
        _notification = new SimpleNotification(1);
        _streamRequest = new SimpleStreamRequest(10);
    }

    [Benchmark]
    public async Task Send_SimpleRequest()
    {
        await _mediator.Send(_request);
    }

    [Benchmark]
    public async Task Publish_SimpleNotification()
    {
        await _mediator.Publish(_notification);
    }

    [Benchmark]
    public async Task Send_RequestWithBehavior()
    {
        await _mediator.Send(_request);
    }

    [Benchmark]
    public async Task SendStream_SimpleRequest()
    {
        await foreach (var response1 in _mediator.SendStream<SimpleStreamResponse>(_streamRequest))
        await foreach (var response2 in _mediator.SendStream<SimpleStreamResponse>(_streamRequest))
        await foreach (var response3 in _mediator.SendStream<SimpleStreamResponse>(_streamRequest))
        {
            // Consume the stream
        }
    }
} 