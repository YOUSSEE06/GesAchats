using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface ISuiviFournisseurService
{
    Task<SuiviFournisseurKpisDto> GetSuiviKpisAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PagedResult<FournisseurSuiviDto>> SearchFournisseursAsync(string searchText, int pageNumber, int pageSize);
    Task<SituationFournisseurDto> GetSituationFournisseurAsync(int fournisseurId, DateTime? startDate = null, DateTime? endDate = null);
}
