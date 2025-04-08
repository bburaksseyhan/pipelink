using Pipelink.Interfaces;

namespace Pipelink.Handlers;

public interface INotificationHandler<TNotification> 
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
