using IstpetDev.Application.PublicTrace.Services;
using IstpetDev.Application.ServicePoints.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IServicePointsService, ServicePointsService>();
        services.AddScoped<IPublicTraceService, PublicTraceService>();

        return services;
    }
}
