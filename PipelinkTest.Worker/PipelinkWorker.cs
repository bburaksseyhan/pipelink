using Pipelink.Interfaces;
using PipelinkTest.Worker.Commands;

namespace PipelinkTest.Worker;

/// <summary>
/// Sends a <see cref="ProcessTickCommand"/> on an interval. Each tick runs in its own DI scope so handlers
/// can depend on scoped services such as an EF Core DbContext.
/// </summary>
public sealed class PipelinkWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelinkWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;

    public PipelinkWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PipelinkWorker> logger,
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
        var runOnce = _configuration.GetValue("PipelinkWorker:RunOnce", false);
        var intervalSeconds = _configuration.GetValue("PipelinkWorker:IntervalSeconds", 5);
        var tick = 0;

        do
        {
            tick++;
            using (var scope = _scopeFactory.CreateScope())
            {
                var pipelink = scope.ServiceProvider.GetRequiredService<IPipelink>();
                var result = await pipelink.Send(new ProcessTickCommand(tick), stoppingToken);
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
