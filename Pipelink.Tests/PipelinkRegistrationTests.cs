using Microsoft.Extensions.DependencyInjection;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Tests;

public class PipelinkRegistrationTests
{
    [Fact]
    public void AddPipelink_ShouldRegisterMediatorAndScannedHandlers_WhenAssemblyScanConfigured()
    {
        var services = new ServiceCollection();
        services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<RegistrationRequest>());

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<Pipelink.Implementation.Pipelink>());
        Assert.NotNull(provider.GetService<IRequestHandler<RegistrationRequest, bool>>());
        Assert.NotNull(provider.GetService<INotificationHandler<RegistrationNotification>>());
    }
}

public record RegistrationRequest : IRequest<bool>;

public sealed class RegistrationRequestHandler : IRequestHandler<RegistrationRequest, bool>
{
    public Task<bool> Handle(RegistrationRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}

public record RegistrationNotification : INotification;

public sealed class RegistrationNotificationHandler : INotificationHandler<RegistrationNotification>
{
    public Task Handle(RegistrationNotification notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
