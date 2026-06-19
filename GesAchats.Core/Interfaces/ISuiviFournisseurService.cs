using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface ISuiviFournisseurService
{
    Task<int> GetTotalFournisseursAsync();
    Task<int> GetCommandesEnCoursAsync();
    Task<decimal> GetTotalCommandeAsync();
    Task<decimal> GetSoldeTotalAsync();
    Task<PagedResult<FournisseurSuiviDto>> SearchFournisseursAsync(string searchText, int pageNumber, int pageSize);
    Task<SituationFournisseurDto> GetSituationFournisseurAsync(int fournisseurId);
}
