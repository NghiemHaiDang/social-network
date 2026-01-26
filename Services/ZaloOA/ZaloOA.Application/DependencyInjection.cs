using Microsoft.Extensions.DependencyInjection;
using ZaloOA.Application.Interfaces;
using ZaloOA.Application.Services;

namespace ZaloOA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IZaloOAService, ZaloOAService>();

        return services;
    }
}
