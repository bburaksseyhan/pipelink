using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace PipelinkTest.Api.Notifications;

public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
}

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Email] Welcome email sent to user {notification.UserId}");
        return Task.CompletedTask;
    }
}
