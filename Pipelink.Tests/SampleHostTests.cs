using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using PipelinkTest.Api;
using PipelinkTest.ConsoleApp.Queries;
using PipelinkTest.ConsoleApp.QueryHandlers;
using PipelinkTest.Worker.CommandHandlers;
using PipelinkTest.Worker.Commands;

namespace Pipelink.Tests;

public class SampleHostTests
{
    [Fact]
    public async Task ConsoleSample_ShouldReturnGreeting_WhenGreetQuerySent()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<GreetQueryHandler>());
        var provider = services.BuildServiceProvider();
        var pipelink = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();

        var result = await pipelink.Send(new GreetQuery("QA"));

        Assert.Equal("Hello, QA!", result);
    }

    [Fact]
    public async Task WorkerSample_ShouldDoubleTick_WhenProcessTickCommandSent()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessTickCommandHandler>());
        var provider = services.BuildServiceProvider();
        var pipelink = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();

        var result = await pipelink.Send(new ProcessTickCommand(7));

        Assert.Equal(14, result);
    }

    [Fact]
    public async Task ApiSample_ShouldReturnUser_WhenGetUserEndpointCalled()
    {
        await using var factory = new WebApplicationFactory<ApiApplicationEntry>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/user/42");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"id\":42", json);
        Assert.Contains("John Doe", json);
    }
}
