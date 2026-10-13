using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.PublicTrace.Dtos;

namespace IstpetDev.Application.PublicTrace.Services;

public abstract record PublicTraceResult
{
    public sealed record Success(PublicTraceResponseDto Trace) : PublicTraceResult;
    public sealed record RateLimited() : PublicTraceResult;
    public sealed record NotFound() : PublicTraceResult;
}

public interface IPublicTraceService
{
    Task<PublicTraceResult> GetTraceAsync(string unitId, string? token, CancellationToken cancellationToken = default);
}
