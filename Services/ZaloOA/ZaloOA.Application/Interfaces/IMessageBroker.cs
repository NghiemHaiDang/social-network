namespace ZaloOA.Application.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync<T>(string exchangeName, string routingKey, T message) where T : class;
}
