using BuildingBlocks.EventBus.Events;
using BuildingBlocks.EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventBus;

/// <summary>
/// In-memory implementation of event bus for testing and simple scenarios
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(
        ISubscriptionManager subscriptionManager,
        IServiceProvider serviceProvider,
        ILogger<InMemoryEventBus> logger)
    {
        _subscriptionManager = subscriptionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var eventName = @event.GetType().Name;
        _logger.LogInformation("Publishing event {EventName} with Id {EventId}", eventName, @event.Id);

        if (!_subscriptionManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("No subscriptions for event {EventName}", eventName);
            return;
        }

        var handlers = _subscriptionManager.GetHandlersForEvent(eventName);

        using var scope = _serviceProvider.CreateScope();

        foreach (var handlerType in handlers)
        {
            try
            {
                var handler = scope.ServiceProvider.GetService(handlerType);
                if (handler == null)
                {
                    _logger.LogWarning("Handler {HandlerType} not found in DI container", handlerType.Name);
                    continue;
                }

                var concreteType = typeof(IEventHandler<>).MakeGenericType(typeof(TEvent));
                var method = concreteType.GetMethod("HandleAsync");

                if (method != null)
                {
                    await (Task)method.Invoke(handler, new object[] { @event, cancellationToken })!;
                    _logger.LogInformation("Event {EventName} handled by {HandlerType}", eventName, handlerType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventName} with handler {HandlerType}",
                    eventName, handlerType.Name);
                throw;
            }
        }
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionManager.GetEventKey<TEvent>();
        _logger.LogInformation("Subscribing {HandlerType} to event {EventName}", typeof(THandler).Name, eventName);

        _subscriptionManager.AddSubscription<TEvent, THandler>();
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionManager.GetEventKey<TEvent>();
        _logger.LogInformation("Unsubscribing {HandlerType} from event {EventName}", typeof(THandler).Name, eventName);

        _subscriptionManager.RemoveSubscription<TEvent, THandler>();
    }
}
