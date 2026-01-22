using System.Text;
using System.Text.Json;
using BuildingBlocks.EventBus.Events;
using BuildingBlocks.EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IRabbitMQConnection _connection;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly RabbitMQSettings _settings;
    private IModel? _consumerChannel;
    private bool _disposed;

    public RabbitMQEventBus(
        IRabbitMQConnection connection,
        ISubscriptionManager subscriptionManager,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger,
        RabbitMQSettings settings)
    {
        _connection = connection;
        _subscriptionManager = subscriptionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings;

        _subscriptionManager.OnEventRemoved += OnEventRemoved;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        var eventName = @event.GetType().Name;

        _logger.LogInformation("Publishing event {EventName} to RabbitMQ", eventName);

        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Direct,
            durable: true);

        var message = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.MessageId = @event.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: eventName,
            mandatory: true,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Event {EventName} published successfully", eventName);

        return Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionManager.GetEventKey<TEvent>();

        _logger.LogInformation("Subscribing to event {EventName} with {Handler}", eventName, typeof(THandler).Name);

        _subscriptionManager.AddSubscription<TEvent, THandler>();

        StartBasicConsume(eventName);
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionManager.GetEventKey<TEvent>();

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subscriptionManager.RemoveSubscription<TEvent, THandler>();
    }

    private void StartBasicConsume(string eventName)
    {
        if (_consumerChannel != null)
            return;

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        _consumerChannel = _connection.CreateModel();

        _consumerChannel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Direct,
            durable: true);

        _consumerChannel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _consumerChannel.QueueBind(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: eventName);

        _consumerChannel.CallbackException += (sender, args) =>
        {
            _logger.LogWarning(args.Exception, "Consumer channel callback exception");
            _consumerChannel?.Dispose();
            _consumerChannel = null;
            StartBasicConsume(eventName);
        };

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

        consumer.Received += async (model, ea) =>
        {
            var receivedEventName = ea.RoutingKey;
            var message = Encoding.UTF8.GetString(ea.Body.Span);

            try
            {
                await ProcessEvent(receivedEventName, message);
                _consumerChannel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventName}", receivedEventName);
                _consumerChannel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _consumerChannel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        _logger.LogInformation("Processing event {EventName}", eventName);

        if (!_subscriptionManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("No subscriptions for event {EventName}", eventName);
            return;
        }

        var eventType = _subscriptionManager.GetEventTypeByName(eventName);
        if (eventType == null)
        {
            _logger.LogWarning("Event type not found for {EventName}", eventName);
            return;
        }

        var @event = JsonSerializer.Deserialize(message, eventType);
        if (@event == null)
        {
            _logger.LogWarning("Failed to deserialize event {EventName}", eventName);
            return;
        }

        var handlers = _subscriptionManager.GetHandlersForEvent(eventName);

        using var scope = _serviceProvider.CreateScope();

        foreach (var handlerType in handlers)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler == null)
            {
                _logger.LogWarning("Handler {HandlerType} not found", handlerType.Name);
                continue;
            }

            var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
            var method = concreteType.GetMethod("HandleAsync");

            if (method != null)
            {
                await (Task)method.Invoke(handler, new[] { @event, CancellationToken.None })!;
                _logger.LogInformation("Event {EventName} handled by {Handler}", eventName, handlerType.Name);
            }
        }
    }

    private void OnEventRemoved(object? sender, string eventName)
    {
        if (!_connection.IsConnected)
            return;

        using var channel = _connection.CreateModel();
        channel.QueueUnbind(
            queue: _settings.QueueName,
            exchange: _settings.ExchangeName,
            routingKey: eventName);

        if (_subscriptionManager.IsEmpty)
        {
            _consumerChannel?.Close();
            _consumerChannel = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _consumerChannel?.Dispose();
    }
}
