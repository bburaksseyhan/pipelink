using Pipelink.Handlers;

namespace Pipelink.Benchmarks;

public class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
} 