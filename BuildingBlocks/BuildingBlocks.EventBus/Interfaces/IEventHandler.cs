using BuildingBlocks.EventBus.Events;

namespace BuildingBlocks.EventBus.Interfaces;

/// <summary>
/// Interface for handling integration events
/// </summary>
/// <typeparam name="TEvent">The type of integration event to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
