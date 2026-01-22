using BuildingBlocks.EventBus.Interfaces;
using BuildingBlocks.EventBus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace BuildingBlocks.EventBus;

public static class DependencyInjection
{
    /// <summary>
    /// Adds in-memory event bus services to the DI container
    /// </summary>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ event bus services to the DI container
    /// </summary>
    public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, Action<RabbitMQSettings> configure)
    {
        var settings = new RabbitMQSettings();
        configure(settings);

        services.AddSingleton(settings);

        services.AddSingleton<IConnectionFactory>(sp =>
        {
            return new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                DispatchConsumersAsync = true
            };
        });

        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMQConnection>>();
            return new RabbitMQConnection(factory, logger, settings.RetryCount);
        });

        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IEventBus, RabbitMQEventBus>();

        return services;
    }

    /// <summary>
    /// Registers an event handler in the DI container
    /// </summary>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : Events.IntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddTransient<THandler>();
        return services;
    }
}
