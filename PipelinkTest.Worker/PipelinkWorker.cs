using PipelinkTest.Worker.Commands;

namespace PipelinkTest.Worker;

public sealed class PipelinkWorker : BackgroundService
{
    private readonly Pipelink.Implementation.Pipelink _pipelink;
    private readonly ILogger<PipelinkWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;

    public PipelinkWorker(
        Pipelink.Implementation.Pipelink pipelink,
        ILogger<PipelinkWorker> logger,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime)
    {
        _pipelink = pipelink;
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
            var result = await _pipelink.Send(new ProcessTickCommand(tick), stoppingToken);
            _logger.LogInformation("ProcessTickCommand tick={Tick} result={Result}", tick, result);

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
