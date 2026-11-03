using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pipelink.Extensions;
using PipelinkTest.ConsoleApp.Queries;
using PipelinkTest.ConsoleApp.QueryHandlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPipelink(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<GreetQueryHandler>());

using var host = builder.Build();
var pipelink = host.Services.GetRequiredService<Pipelink.Implementation.Pipelink>();

var name = args.Length > 0 ? args[0] : "Pipelink";
var greeting = await pipelink.Send(new GreetQuery(name));

Console.WriteLine(greeting);
return greeting.StartsWith("Hello,", StringComparison.Ordinal) ? 0 : 1;
