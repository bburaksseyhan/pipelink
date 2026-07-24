using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace PipelinkTest.Api.Features.Users;

public record UserCreatedNotification(int UserId, string Email) : INotification;

/// <summary>
/// First of two handlers for the same notification; Pipelink.Publish invokes both in registration order.
/// </summary>
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(ILogger<SendWelcomeEmailHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Welcome email sent to {Email} (user {UserId})", notification.Email, notification.UserId);
        return Task.CompletedTask;
    }
}

public class InvalidateUserCacheHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ILogger<InvalidateUserCacheHandler> _logger;

    public InvalidateUserCacheHandler(ILogger<InvalidateUserCacheHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User cache invalidated for user {UserId}", notification.UserId);
        return Task.CompletedTask;
    }
}
