using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orient.MediatR.Extensions;
using Orient.MediatR.Interfaces;
using Orient.MediatR.SampleConsole.Queries;
using Orient.MediatR.SampleConsole.QueryHandlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOrientMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<GreetQueryHandler>());

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

var name = args.Length > 0 ? args[0] : "Orient";
var greeting = await mediator.Send(new GreetQuery(name));

Console.WriteLine(greeting);
return greeting.StartsWith("Hello,", StringComparison.Ordinal) ? 0 : 1;
