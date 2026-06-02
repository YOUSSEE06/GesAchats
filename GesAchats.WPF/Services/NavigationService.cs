using System;
using GesAchats.Core.Entities;

namespace GesAchats.WPF.Services;

public interface INavigationService
{
    void NavigateTo(string pageName, object? parameter = null);
    void GoBack();
    event Action<string, object?>? OnNavigate;
}

public interface INavigatable
{
    void OnNavigatedTo(object parameter);
}

public class BonCommandeCreationContext
{
    public Supplier Supplier { get; set; } = null!;
    public Quotation Quotation { get; set; } = null!;
    public List<PurchaseOrderDetail>? Items { get; set; }
}

public class NavigationService : INavigationService
{
    public event Action<string, object?>? OnNavigate;

    public void NavigateTo(string pageName, object? parameter = null)
    {
        OnNavigate?.Invoke(pageName, parameter);
    }

    public void GoBack()
    {
        // For now, we don't have a navigation stack, but we can leave this for future expansion
    }
}
