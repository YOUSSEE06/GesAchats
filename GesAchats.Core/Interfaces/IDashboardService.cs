using System.Threading.Tasks;
using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetMagasinierDashboardStatsAsync(int days = 30);
}
