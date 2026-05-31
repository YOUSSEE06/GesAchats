using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class NeedsService : INeedsService
{
    private readonly IUnitOfWork _unitOfWork;

    public NeedsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> GetPendingNeedsCountAsync(DateTime startDate, DateTime endDate)
    {
        var needs = await _unitOfWork.Needs.FindAsync(n =>
            (n.Status == NeedStatus.ToValidate || n.Status == NeedStatus.TransmittedToPurchasing) &&
            n.RequestedAt >= startDate &&
            n.RequestedAt <= endDate);
        return needs.Count();
    }
}
