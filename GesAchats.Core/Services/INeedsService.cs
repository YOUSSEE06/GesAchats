namespace GesAchats.Core.Services;

public interface INeedsService
{
    Task<int> GetPendingNeedsCountAsync(DateTime startDate, DateTime endDate);
}
