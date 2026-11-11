using Orient.MediatR.Extensions;
using Orient.MediatR.SampleWorker;
using Orient.MediatR.SampleWorker.CommandHandlers;

var builder = Host.CreateApplicationBuilder(args);

if (args.Contains("--run-once", StringComparer.OrdinalIgnoreCase))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["TickWorker:RunOnce"] = "true"
    });
}

builder.Services.AddOrientMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
builder.Services.AddHostedService<TickWorker>();

var host = builder.Build();
await host.RunAsync();
