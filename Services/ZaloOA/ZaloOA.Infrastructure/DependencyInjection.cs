using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Infrastructure.Configuration;
using ZaloOA.Infrastructure.ExternalServices.Zalo;
using ZaloOA.Infrastructure.Persistence;
using ZaloOA.Infrastructure.Persistence.Repositories;

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
        var zaloConfiguration = new ZaloConfiguration();
        configuration.GetSection("Zalo").Bind(zaloConfiguration);
        services.AddSingleton(zaloConfiguration);
        services.AddSingleton<IZaloConfiguration>(zaloConfiguration);

        // Repositories
        services.AddScoped<IZaloOAAccountRepository, ZaloOAAccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // External Services
        services.AddHttpClient<IZaloApiService, ZaloApiService>();

        return services;
    }
}
