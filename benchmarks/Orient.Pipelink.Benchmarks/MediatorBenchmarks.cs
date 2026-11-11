using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Orient.Pipelink.Extensions;

namespace Orient.Pipelink.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 5, warmupCount: 3, iterationCount: 10)]
public class MediatorBenchmarks
{
    private readonly MediatR.IMediator _mediatR;
    private readonly Implementation.Pipelink _pipelink;
    private readonly TestRequest _request;

    public MediatorBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmarks).Assembly));
        var provider = services.BuildServiceProvider();

        _mediatR = provider.GetRequiredService<MediatR.IMediator>();
        _pipelink = provider.GetRequiredService<Implementation.Pipelink>();
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

public class TestRequest : MediatR.IRequest<string>, Interfaces.IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class TestRequestHandler :
    MediatR.IRequestHandler<TestRequest, string>,
    Handlers.IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Message);
    }
}
