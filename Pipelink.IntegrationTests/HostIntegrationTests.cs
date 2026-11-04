using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipelink.Behaviors;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;
using PipelinkTest.ConsoleApp.Queries;
using PipelinkTest.ConsoleApp.QueryHandlers;
using PipelinkTest.Worker;
using PipelinkTest.Worker.CommandHandlers;

namespace Pipelink.IntegrationTests;

public class HostIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Worker_ShouldProcessOneTickAndStop_WhenRunOnceConfigured()
    {
        var logs = new LogCollector();
        var builder = CreateWorkerBuilder(logs, runOnce: true);
        using var host = builder.Build();

        await host.RunAsync().WaitAsync(Timeout);

        Assert.Contains("ProcessTickCommand tick=1 result=2", logs.Messages);
    }

    [Fact]
    public async Task Worker_ShouldKeepProcessingTicks_UntilHostStopped()
    {
        var logs = new LogCollector();
        var builder = CreateWorkerBuilder(logs, runOnce: false);
        using var host = builder.Build();

        await host.StartAsync();
        await logs.WaitForAsync("tick=3 result=6", Timeout);
        await host.StopAsync().WaitAsync(Timeout);

        Assert.Contains("ProcessTickCommand tick=2 result=4", logs.Messages);
    }

    [Fact]
    public async Task ConsoleHost_ShouldResolvePipelinkAndSend_WhenBuiltLikeSample()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<GreetQueryHandler>());
        using var host = builder.Build();

        var greeting = await host.Services.GetRequiredService<Implementation.Pipelink>().Send(new GreetQuery("QA"));

        Assert.Equal("Hello, QA!", greeting);
    }

    [Fact]
    public async Task Send_ShouldStayCorrect_WhenManyRequestTypesSentConcurrentlyOnColdMediator()
    {
        using var host = CreateTestAssemblyBuilder().Build();
        var pipelink = host.Services.GetRequiredService<Implementation.Pipelink>();

        var tasks = Enumerable.Range(0, 400).Select(async i =>
        {
            await Task.Yield();
            return i % 2 == 0
                ? (object)await pipelink.Send(new EchoText($"t{i}"))
                : await pipelink.Send(new EchoNumber(i));
        });
        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < results.Length; i++)
        {
            Assert.Equal(i % 2 == 0 ? $"t{i}" : i, results[i]);
        }
    }

    [Fact]
    public async Task Send_ShouldRunCustomBehaviorAroundDefaults_WhenRegisteredInHost()
    {
        var builder = CreateTestAssemblyBuilder();
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SuffixBehavior<,>));
        using var host = builder.Build();

        var result = await host.Services.GetRequiredService<Implementation.Pipelink>().Send(new EchoText("x"));

        Assert.Equal("x!", result);
    }

    [Fact]
    public async Task Send_ShouldResolveScopedHandlerDependency_WhenCalledInsideScope()
    {
        using var host = CreateTestAssemblyBuilder().Build();
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RequestContext>().User = "alice";

        var user = await scope.ServiceProvider.GetRequiredService<Implementation.Pipelink>().Send(new WhoAmI());

        Assert.Equal("alice", user);
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

    private static HostApplicationBuilder CreateTestAssemblyBuilder()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<HostIntegrationTests>());
        builder.Services.AddScoped<RequestContext>();
        return builder;
    }

    private static HostApplicationBuilder CreateWorkerBuilder(LogCollector logs, bool runOnce)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PipelinkWorker:RunOnce"] = runOnce.ToString(),
            ["PipelinkWorker:IntervalSeconds"] = "0"
        });
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
        builder.Services.AddHostedService<PipelinkWorker>();
        return builder;
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

    private sealed class SuffixBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
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
