using Pipelink.Extensions;
using PipelinkTest.Worker;
using PipelinkTest.Worker.CommandHandlers;

var builder = Host.CreateApplicationBuilder(args);

if (args.Contains("--run-once", StringComparer.OrdinalIgnoreCase))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PipelinkWorker:RunOnce"] = "true"
    });
}

builder.Services.AddPipelink(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
builder.Services.AddHostedService<PipelinkWorker>();

var host = builder.Build();
await host.RunAsync();
