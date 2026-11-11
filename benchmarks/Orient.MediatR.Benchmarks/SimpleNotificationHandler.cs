using Orient.MediatR.Handlers;

namespace Orient.MediatR.Benchmarks;

public class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
} 