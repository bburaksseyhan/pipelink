using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using Pipelink.Implementation;

namespace Pipelink.Benchmarks;

[MemoryDiagnoser]
public class PipelinkBenchmarks
{
    private Implementation.Pipelink _mediator = null!;
    private SimpleRequest _request = null!;
    private SimpleNotification _notification = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => 
        {
            cfg.RegisterServicesFromAssemblyContaining<PipelinkBenchmarks>();
        });

        var serviceProvider = services.BuildServiceProvider();
        _mediator = serviceProvider.GetRequiredService<Implementation.Pipelink>();
        _request = new SimpleRequest(1);
        _notification = new SimpleNotification(1);
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
} 