using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Tests;

public class PipelinkStreamTests
{
    [Fact]
    public async Task SendStream_ShouldYieldAllItems_WhenStreamHandlerRegistered()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<PipelinkStreamTests>());
        services.AddTransient<IStreamRequestHandler<CountStreamRequest, int>, CountStreamHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();

        var items = new List<int>();
        await foreach (var item in mediator.SendStream(new CountStreamRequest(3)))
        {
            items.Add(item);
        }

        Assert.Equal([1, 2, 3], items);
    }

    public record CountStreamRequest(int Count) : IStreamRequest<int>;

    private sealed class CountStreamHandler : IStreamRequestHandler<CountStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(
            CountStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }
    }
}
