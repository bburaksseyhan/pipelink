using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using Pipelink.Interfaces;
using Pipelink.Handlers;

namespace Pipelink.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 5, warmupCount: 3, iterationCount: 10)]
public class MediatorBenchmarks
{
    private readonly IMediator _mediatR;
    private readonly Pipelink.Implementation.Pipelink _pipelink;
    private readonly TestRequest _request;

    public MediatorBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        var provider = services.BuildServiceProvider();

        _mediatR = provider.GetRequiredService<IMediator>();
        _pipelink = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();
        _request = new TestRequest { Message = "Test" };
    }

    [Benchmark(Baseline = true)]
    public async Task<string> MediatR_Send()
    {
        return await _mediatR.Send(_request);
    }

    [Benchmark]
    public async Task<string> Pipelink_Send()
    {
        return await _pipelink.Send(_request);
    }
}

public class TestRequest : MediatR.IRequest<string>, Pipelink.Interfaces.IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestHandler : 
    MediatR.IRequestHandler<TestRequest, string>,
    Pipelink.Handlers.IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Message);
    }
} 