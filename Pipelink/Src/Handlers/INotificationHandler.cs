using Pipelink.Interfaces;

namespace Pipelink.Handlers;

/// <summary>
/// Defines a contract for handling notifications of a specific type.
/// </summary>
/// <typeparam name="TNotification">
/// The type of notification to be handled. Must implement the <see cref="INotification"/> interface.
/// </typeparam>
public interface INotificationHandler<in TNotification> 
    where TNotification : INotification
{
    /// <summary>
    /// Handles the given notification.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification to handle, must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification instance to be handled.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, enabling task cancellation.</param>
    /// <returns>A task that represents the asynchronous operation of handling the notification.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
