using IstpetDev.Application.PublicTrace.Services;
using IstpetDev.Application.Agent;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Application.Logistics.Policy;
using IstpetDev.Application.ServicePoints.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IServicePointsService, ServicePointsService>();
        services.AddSingleton<IPublicTraceRateLimiter, PublicTraceRateLimiter>();
        services.AddScoped<IPublicTraceService, PublicTraceService>();
        services.AddScoped<IstpetDev.Application.Field.IFieldService, IstpetDev.Application.Field.FieldService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IOperationalClock, OperationalClock>();
        services.AddScoped<IRiskCalculator, RiskCalculator>();
        services.AddScoped<ReplenishmentAllocator>();
        services.AddHttpClient<IDistanceProvider, DistanceProvider>();
        services.AddScoped<IRoutePlanner, RoutePlanner>();
        services.AddScoped<IRoutePublisher, RoutePublisher>();
        services.AddScoped<IIncidentEvaluator, IncidentEvaluator>();
        services.AddScoped<IReplanner, Replanner>();
        services.AddSingleton<AutonomyState>();
        services.AddScoped<IAutonomyPolicy, AutonomyPolicy>();
        services.AddSingleton<IExplanationTemplates, ExplanationTemplates>();
        services.AddSingleton<IExplanationValidator, ExplanationValidator>();
        services.AddScoped<IAgentRunService, AgentRunService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
