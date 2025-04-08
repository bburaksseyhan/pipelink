using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Behaviors;
using Pipelink.Handlers;
using Pipelink.Interfaces;

namespace Pipelink.Implementation;

public class Pipelink(IServiceProvider serviceProvider)
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
            (Task<TResponse>)handlerType
                .GetMethod("Handle")!
                .Invoke(handler, [request, cancellationToken])!;

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = serviceProvider.GetServices(behaviorType).Cast<dynamic>().ToList();

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle((dynamic)request, cancellationToken, next);
        }

        return await handlerDelegate();
    }
    
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = serviceProvider
            .GetServices<INotificationHandler<TNotification>>()
            .ToList();
        
        // foreach (var handler in handlers)
        // {
        //     await handler.Handle(notification, cancellationToken);
        // }
        await Task.WhenAll(handlers.Select(h => h.Handle(notification, cancellationToken)));
    }
}