using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.ServicePoints.Dtos;

namespace IstpetDev.Application.ServicePoints.Services;

public interface IServicePointsService
{
    Task<IReadOnlyList<ServicePointDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
