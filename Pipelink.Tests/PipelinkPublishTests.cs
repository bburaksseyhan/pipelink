using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Tests;

public class PipelinkPublishTests
{
    [Fact]
    public async Task Publish_ShouldInvokeAllNotificationHandlers_WhenMultipleHandlersRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationSink>();
        services.AddTransient<INotificationHandler<SampleNotification>, HandlerA>();
        services.AddTransient<INotificationHandler<SampleNotification>, HandlerB>();
        services.AddPipelink();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<Pipelink.Implementation.Pipelink>();
        var sink = provider.GetRequiredService<NotificationSink>();

        await mediator.Publish(new SampleNotification(42));

        Assert.Equal(2, sink.Count);
        Assert.Contains("A:42", sink.Messages);
        Assert.Contains("B:42", sink.Messages);
    }

    public record SampleNotification(int Id) : INotification;

    private sealed class NotificationSink
    {
        public List<string> Messages { get; } = [];
        public int Count => Messages.Count;
    }

    private sealed class HandlerA : INotificationHandler<SampleNotification>
    {
        private readonly NotificationSink _sink;
        public HandlerA(NotificationSink sink) => _sink = sink;

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _sink.Messages.Add($"A:{notification.Id}");
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerB : INotificationHandler<SampleNotification>
    {
        private readonly NotificationSink _sink;
        public HandlerB(NotificationSink sink) => _sink = sink;

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _sink.Messages.Add($"B:{notification.Id}");
            return Task.CompletedTask;
        }
    }
}
