using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orient.Pipelink.Behaviors;
using Orient.Pipelink.Extensions;
using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;
using Orient.Pipelink.SampleConsole.Queries;
using Orient.Pipelink.SampleConsole.QueryHandlers;
using Orient.Pipelink.SampleWorker;
using Orient.Pipelink.SampleWorker.CommandHandlers;

namespace Orient.Pipelink.IntegrationTests;

public class HostIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Worker_ShouldProcessOneTickAndStop_WhenRunOnceConfigured()
    {
        var logs = new LogCollector();
        using var host = CreateWorkerBuilder(logs, runOnce: true).Build();

        await host.RunAsync().WaitAsync(Timeout);

        Assert.Contains("ProcessTickCommand tick=1 result=2", logs.Messages);
    }

    [Fact]
    public async Task Worker_ShouldKeepProcessingTicks_UntilHostStopped()
    {
        var logs = new LogCollector();
        using var host = CreateWorkerBuilder(logs, runOnce: false).Build();

        await host.StartAsync();
        await logs.WaitForAsync("tick=3 result=6", Timeout);
        await host.StopAsync().WaitAsync(Timeout);

        Assert.Contains("ProcessTickCommand tick=2 result=4", logs.Messages);
    }

    [Fact]
    public async Task ConsoleHost_ShouldSendQuery_WhenBuiltLikeSample()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<GreetQueryHandler>());
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var greeting = await scope.ServiceProvider.GetRequiredService<IPipelink>().Send(new GreetQuery("QA"));

        Assert.Equal("Hello, QA!", greeting);
    }

    [Fact]
    public async Task Send_ShouldResolveScopedHandlerDependency_WhenCalledInsideScope()
    {
        using var host = CreateTestAssemblyBuilder().Build();
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RequestContext>().User = "alice";

        var user = await scope.ServiceProvider.GetRequiredService<IPipelink>().Send(new WhoAmI());

        Assert.Equal("alice", user);
    }

    [Fact]
    public async Task Send_ShouldStayCorrect_WhenManyRequestTypesSentConcurrentlyWithCaching()
    {
        using var host = CreateTestAssemblyBuilder(cfg => cfg.AddOpenBehavior(typeof(CachingBehavior<,>))).Build();
        var pipelink = host.Services.GetRequiredService<IPipelink>();

        var results = await Task.WhenAll(Enumerable.Range(0, 400).Select(async i =>
        {
            await Task.Yield();
            return i % 2 == 0
                ? (object)await pipelink.Send(new EchoText($"t{i % 20}"))
                : await pipelink.Send(new EchoNumber(i % 20));
        }));

        for (var i = 0; i < results.Length; i++)
        {
            Assert.Equal(i % 2 == 0 ? $"t{i % 20}" : i % 20, results[i]);
        }
    }

    [Fact]
    public async Task Send_ShouldRunOptInBehavior_WhenRegisteredWithAddOpenBehavior()
    {
        using var host = CreateTestAssemblyBuilder(cfg => cfg.AddOpenBehavior(typeof(SuffixBehavior<,>))).Build();

        var result = await host.Services.GetRequiredService<IPipelink>().Send(new EchoText("x"));

        Assert.Equal("x!", result);
    }

    private static HostApplicationBuilder CreateTestAssemblyBuilder(Action<PipelinkConfiguration>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Services.AddPipelink(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<HostIntegrationTests>();
            configure?.Invoke(cfg);
        });
        builder.Services.AddScoped<RequestContext>();
        return builder;
    }

    private static HostApplicationBuilder CreateWorkerBuilder(LogCollector logs, bool runOnce)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TickWorker:RunOnce"] = runOnce.ToString(),
            ["TickWorker:IntervalSeconds"] = "0"
        });
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
        builder.Services.AddHostedService<TickWorker>();
        return builder;
    }

    public sealed class RequestContext
    {
        public string User { get; set; } = "anonymous";
    }

    public record WhoAmI : IRequest<string>;

    public sealed class WhoAmIHandler(RequestContext context) : IRequestHandler<WhoAmI, string>
    {
        public Task<string> Handle(WhoAmI request, CancellationToken cancellationToken) => Task.FromResult(context.User);
    }

    public record EchoText(string Value) : IRequest<string>;

    public record EchoNumber(int Value) : IRequest<int>;

    public sealed class EchoTextHandler : IRequestHandler<EchoText, string>
    {
        public async Task<string> Handle(EchoText request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return request.Value;
        }
    }

    public sealed class EchoNumberHandler : IRequestHandler<EchoNumber, int>
    {
        public Task<int> Handle(EchoNumber request, CancellationToken cancellationToken) => Task.FromResult(request.Value);
    }

    public sealed class SuffixBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var response = await next();
            return response is string text ? (TResponse)(object)(text + "!") : response;
        }
    }

    private sealed class LogCollector : ILoggerProvider, ILogger
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages
        {
            get { lock (_messages) return [.. _messages]; }
        }

        public async Task WaitForAsync(string fragment, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            while (!Messages.Any(m => m.Contains(fragment, StringComparison.Ordinal)))
            {
                await Task.Delay(10, cts.Token);
            }
        }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_messages) _messages.Add(formatter(state, exception));
        }

        public void Dispose()
        {
        }
    }
}
