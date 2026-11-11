using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Orient.MediatR.Extensions;
using LuckyPennyMediator = global::MediatR.IMediator;

namespace Orient.MediatR.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 5, warmupCount: 3, iterationCount: 10)]
public class MediatorBenchmarks
{
    private readonly LuckyPennyMediator _mediatR;
    private readonly Implementation.Mediator _mediator;
    private readonly TestRequest _request;

    public MediatorBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        services.AddOrientMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        var provider = services.BuildServiceProvider();

        _mediatR = provider.GetRequiredService<LuckyPennyMediator>();
        _mediator = provider.GetRequiredService<Implementation.Mediator>();
        _request = new TestRequest { Message = "Test" };
    }

    [Benchmark(Baseline = true)]
    public async Task<string> MediatR_Send()
    {
        return await _mediatR.Send(_request);
    }

    [Benchmark]
    public async Task<string> OrientMediatR_Send()
    {
        return await _mediator.Send(_request);
    }
}

public class TestRequest : global::MediatR.IRequest<string>, Interfaces.IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestHandler : 
    global::MediatR.IRequestHandler<TestRequest, string>,
    Handlers.IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Message);
    }
} 