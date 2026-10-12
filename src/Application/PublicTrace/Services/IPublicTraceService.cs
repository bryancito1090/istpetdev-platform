using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.PublicTrace.Dtos;

namespace IstpetDev.Application.PublicTrace.Services;

public interface IPublicTraceService
{
    Task<PublicTraceDto?> GetTraceAsync(string unitId, CancellationToken cancellationToken = default);
}
