using APBD_Test1.Models;

namespace APBD_Test1.Services;

public interface IDbService
{
    public Task<Visit> GetVisitByIdAsync(int visitId, CancellationToken ct);
    public Task<int> AddVisitAsync(VisitRequestDto visitRequestDto, CancellationToken ct);
}