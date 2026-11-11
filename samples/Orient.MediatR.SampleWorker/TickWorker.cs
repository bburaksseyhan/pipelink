using Orient.MediatR.Interfaces;
using Orient.MediatR.SampleWorker.Commands;

namespace Orient.MediatR.SampleWorker;

/// <summary>
/// Sends a <see cref="ProcessTickCommand"/> on an interval. Each tick runs in its own DI scope so handlers
/// can depend on scoped services such as an EF Core DbContext.
/// </summary>
public sealed class TickWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TickWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;

    public TickWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TickWorker> logger,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var runOnce = _configuration.GetValue("TickWorker:RunOnce", false);
        var intervalSeconds = _configuration.GetValue("TickWorker:IntervalSeconds", 5);
        var tick = 0;

        do
        {
            tick++;
            using (var scope = _scopeFactory.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new ProcessTickCommand(tick), stoppingToken);
                _logger.LogInformation("ProcessTickCommand tick={Tick} result={Result}", tick, result);
            }

            if (runOnce)
            {
                _lifetime.StopApplication();
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
