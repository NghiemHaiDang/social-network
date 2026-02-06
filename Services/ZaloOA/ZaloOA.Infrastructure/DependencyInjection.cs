using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ZaloOA.Application.Interfaces;
using ZaloOA.Application.Services;
using ZaloOA.Infrastructure.Data;
using ZaloOA.Infrastructure.Repositories;
using ZaloOA.Infrastructure.Services;

namespace ZaloOA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        var databaseName = configuration.GetConnectionString("DatabaseName") ?? "ZaloOADb";

        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });
        services.AddSingleton<MongoDbContext>();

        // Configuration
        var zaloSettings = new ZaloSettings();
        configuration.GetSection("Zalo").Bind(zaloSettings);
        services.AddSingleton(zaloSettings);

        // Repositories
        services.AddScoped<IZaloOAAccountRepository, ZaloOAAccountRepository>();
        services.AddScoped<IZaloUserRepository, ZaloUserRepository>();
        services.AddScoped<IZaloConversationRepository, ZaloConversationRepository>();
        services.AddScoped<IZaloMessageRepository, ZaloMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // External Services
        services.AddHttpClient<IZaloApiClient, ZaloApiClient>();

        // Message Broker (RabbitMQ)
        var rabbitMQEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
        if (rabbitMQEnabled)
        {
            services.AddSingleton<IMessageBroker, RabbitMQMessageBroker>();
        }
        else
        {
            // No-op implementation when RabbitMQ is disabled
            services.AddSingleton<IMessageBroker, NoOpMessageBroker>();
        }

        return services;
    }
}

/// <summary>
/// No-op implementation when RabbitMQ is disabled
/// </summary>
public class NoOpMessageBroker : IMessageBroker
{
    public Task PublishAsync<T>(string exchangeName, string routingKey, T message) where T : class
    {
        // Do nothing
        return Task.CompletedTask;
    }
}
