namespace IstpetDev.Application.PublicTrace.Services;

/// <summary>
/// Limitador de consultas para la traza pública por token hash (PA-05).
/// </summary>
public interface IPublicTraceRateLimiter
{
    bool IsAllowed(string tokenHash);
}
