using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class MagasinierOrdersViewModel : AdminOrdersViewModel
{
    public MagasinierOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
        : base(unitOfWork, serviceProvider)
    {
        Title = "Gestion des Bons de Commande";
    }
}
