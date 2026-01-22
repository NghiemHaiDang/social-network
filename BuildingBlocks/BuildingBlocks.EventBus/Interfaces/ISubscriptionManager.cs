using BuildingBlocks.EventBus.Events;

namespace BuildingBlocks.EventBus.Interfaces;

/// <summary>
/// Manages event subscriptions
/// </summary>
public interface ISubscriptionManager
{
    bool IsEmpty { get; }

    event EventHandler<string>? OnEventRemoved;

    void AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>;

    void RemoveSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>;

    bool HasSubscriptionsForEvent<TEvent>() where TEvent : IntegrationEvent;

    bool HasSubscriptionsForEvent(string eventName);

    Type? GetEventTypeByName(string eventName);

    void Clear();

    IEnumerable<Type> GetHandlersForEvent<TEvent>() where TEvent : IntegrationEvent;

    IEnumerable<Type> GetHandlersForEvent(string eventName);

    string GetEventKey<TEvent>() where TEvent : IntegrationEvent;
}
