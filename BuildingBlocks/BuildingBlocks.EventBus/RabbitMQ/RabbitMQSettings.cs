namespace BuildingBlocks.EventBus.RabbitMQ;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "event_bus";
    public string QueueName { get; set; } = "event_queue";
    public int RetryCount { get; set; } = 5;
}
