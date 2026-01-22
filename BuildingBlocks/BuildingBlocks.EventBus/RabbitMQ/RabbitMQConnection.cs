using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.EventBus.RabbitMQ;

public class RabbitMQConnection : IRabbitMQConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQConnection> _logger;
    private readonly int _retryCount;
    private IConnection? _connection;
    private bool _disposed;
    private readonly object _syncRoot = new();

    public RabbitMQConnection(
        IConnectionFactory connectionFactory,
        ILogger<RabbitMQConnection> logger,
        int retryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _retryCount = retryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect...");

        lock (_syncRoot)
        {
            if (IsConnected)
                return true;

            var retries = 0;
            while (retries < _retryCount)
            {
                try
                {
                    _connection = _connectionFactory.CreateConnection();

                    if (IsConnected)
                    {
                        _connection.ConnectionShutdown += OnConnectionShutdown;
                        _connection.CallbackException += OnCallbackException;
                        _connection.ConnectionBlocked += OnConnectionBlocked;

                        _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'",
                            _connection.Endpoint.HostName);

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    retries++;
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect. Retry {Retry}/{MaxRetries}",
                        retries, _retryCount);

                    if (retries < _retryCount)
                        Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, retries)));
                }
            }

            _logger.LogError("RabbitMQ Client could not connect after {RetryCount} retries", _retryCount);
            return false;
        }
    }

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to create a model.");
        }

        return _connection!.CreateModel();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning(e.Exception, "RabbitMQ connection callback exception");
        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ connection is shutdown. Reason: {Reason}", e.ReplyText);
        TryConnect();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
