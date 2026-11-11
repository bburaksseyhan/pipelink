using Orient.Pipelink.Extensions;
using Orient.Pipelink.SampleWorker;
using Orient.Pipelink.SampleWorker.CommandHandlers;

var builder = Host.CreateApplicationBuilder(args);

if (args.Contains("--run-once", StringComparer.OrdinalIgnoreCase))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["TickWorker:RunOnce"] = "true"
    });
}

builder.Services.AddPipelink(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
builder.Services.AddHostedService<TickWorker>();

var host = builder.Build();
await host.RunAsync();
