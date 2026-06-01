using System.Collections.Generic;
using System.Threading.Tasks;
using GesAchats.Core.DTOs;
using GesAchats.Core.Services;

namespace GesAchats.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetMagasinierDashboardStatsAsync(int days = 30);
    Task<List<DashboardOperationDto>> GetRecentOperationsAsync(int count = 6);
}
