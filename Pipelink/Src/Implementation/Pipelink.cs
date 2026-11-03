using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Implementation;

/// <summary>
/// Mediator for CQRS request/notification/stream dispatch with an optional behavior pipeline.
/// </summary>
public sealed class Pipelink : IPipelink
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly MethodInfo SendPipelineMethod =
        typeof(Pipelink).GetMethod(nameof(SendPipelineImpl), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public Pipelink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var method = SendPipelineMethod.MakeGenericMethod(requestType, typeof(TResponse));
        var task = (Task<TResponse>)method.Invoke(this, new object[] { request, cancellationToken })!;
        return await task.ConfigureAwait(false);
    }

    private async Task<TResponse> SendPipelineImpl<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        RequestHandlerDelegate<TResponse> next = () =>
            InvokeHandlerAsync<TRequest, TResponse>(request, cancellationToken);

        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = () => behavior.Handle(request, cancellationToken, inner);
        }

        return await next().ConfigureAwait(false);
    }

    private async Task<TResponse> InvokeHandlerAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        return await handler.Handle(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TResponse> SendStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");

        var enumerable = (IAsyncEnumerable<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        await foreach (var response in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return response;
        }
    }

    /// <inheritdoc />
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        foreach (var handler in _serviceProvider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
