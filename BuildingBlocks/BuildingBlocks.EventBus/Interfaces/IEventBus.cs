using BuildingBlocks.EventBus.Events;

namespace BuildingBlocks.EventBus.Interfaces;

/// <summary>
/// Interface for the event bus that handles publishing and subscribing to integration events
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to the event bus
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    /// <summary>
    /// Subscribes a handler to a specific event type
    /// </summary>
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>;

    /// <summary>
    /// Unsubscribes a handler from a specific event type
    /// </summary>
    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>;
}
