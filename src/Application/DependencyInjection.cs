using IstpetDev.Application.PublicTrace.Services;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.ServicePoints.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IServicePointsService, ServicePointsService>();
        services.AddScoped<IPublicTraceService, PublicTraceService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IOperationalClock, OperationalClock>();
        services.AddScoped<IRiskCalculator, RiskCalculator>();
        services.AddScoped<ReplenishmentAllocator>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
