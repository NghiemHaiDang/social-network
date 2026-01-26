using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Database
        services.AddDbContext<ZaloOADbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Configuration
        var zaloSettings = new ZaloSettings();
        configuration.GetSection("Zalo").Bind(zaloSettings);
        services.AddSingleton(zaloSettings);

        // Repositories
        services.AddScoped<IZaloOAAccountRepository, ZaloOAAccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // External Services
        services.AddHttpClient<IZaloApiClient, ZaloApiClient>();

        return services;
    }
}
