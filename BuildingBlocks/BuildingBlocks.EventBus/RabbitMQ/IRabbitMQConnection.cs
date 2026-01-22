using RabbitMQ.Client;

namespace BuildingBlocks.EventBus.RabbitMQ;

public interface IRabbitMQConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();
}
