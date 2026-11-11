using Orient.Pipelink.Handlers;

namespace Orient.Pipelink.Benchmarks;

public class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
} 