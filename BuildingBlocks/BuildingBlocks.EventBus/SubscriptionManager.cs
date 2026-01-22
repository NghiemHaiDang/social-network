using BuildingBlocks.EventBus.Events;
using BuildingBlocks.EventBus.Interfaces;

namespace BuildingBlocks.EventBus;

/// <summary>
/// In-memory implementation of subscription manager
/// </summary>
public class SubscriptionManager : ISubscriptionManager
{
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly List<Type> _eventTypes = new();

    public bool IsEmpty => _handlers.Count == 0;

    public event EventHandler<string>? OnEventRemoved;

    public void AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        var handlerType = typeof(THandler);

        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<Type>());
        }

        if (_handlers[eventName].Contains(handlerType))
        {
            throw new ArgumentException($"Handler {handlerType.Name} already registered for '{eventName}'");
        }

        _handlers[eventName].Add(handlerType);

        if (!_eventTypes.Contains(typeof(TEvent)))
        {
            _eventTypes.Add(typeof(TEvent));
        }
    }

    public void RemoveSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = GetEventKey<TEvent>();
        var handlerType = typeof(THandler);

        if (!HasSubscriptionsForEvent(eventName))
        {
            return;
        }

        _handlers[eventName].Remove(handlerType);

        if (_handlers[eventName].Count == 0)
        {
            _handlers.Remove(eventName);
            _eventTypes.Remove(typeof(TEvent));
            OnEventRemoved?.Invoke(this, eventName);
        }
    }

    public bool HasSubscriptionsForEvent<TEvent>() where TEvent : IntegrationEvent
    {
        var eventName = GetEventKey<TEvent>();
        return HasSubscriptionsForEvent(eventName);
    }

    public bool HasSubscriptionsForEvent(string eventName)
    {
        return _handlers.ContainsKey(eventName);
    }

    public Type? GetEventTypeByName(string eventName)
    {
        return _eventTypes.SingleOrDefault(t => t.Name == eventName);
    }

    public void Clear()
    {
        _handlers.Clear();
        _eventTypes.Clear();
    }

    public IEnumerable<Type> GetHandlersForEvent<TEvent>() where TEvent : IntegrationEvent
    {
        var eventName = GetEventKey<TEvent>();
        return GetHandlersForEvent(eventName);
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName)
    {
        return _handlers.TryGetValue(eventName, out var handlers)
            ? handlers
            : Enumerable.Empty<Type>();
    }

    public string GetEventKey<TEvent>() where TEvent : IntegrationEvent
    {
        return typeof(TEvent).Name;
    }
}
