namespace BuildingBlocks.EventBus.Events;

/// <summary>
/// Base class for integration events used for communication between microservices
/// </summary>
public abstract class IntegrationEvent
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string EventType => GetType().Name;

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected IntegrationEvent(Guid id, DateTime createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }
}
