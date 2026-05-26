using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GesAchats.Core.Entities;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Analyses;

public partial class AdvancedComparativeAnalysisPage : Page
{
    public AdvancedComparativeAnalysisPage(AdvancedComparativeAnalysisViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Écouter les changements dans la liste des fournisseurs sélectionnés pour mettre à jour les colonnes
        viewModel.SelectedSuppliers.CollectionChanged += SelectedSuppliers_CollectionChanged;
    }

    private void SelectedSuppliers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateDynamicColumns();
    }

    private void UpdateDynamicColumns()
    {
        var viewModel = DataContext as AdvancedComparativeAnalysisViewModel;
        if (viewModel == null) return;

        // On garde les colonnes fixes (Produit, Qté, Unité) et la dernière (Meilleur Prix)
        // Les colonnes fixes sont aux index 0, 1, 2. La colonne "Meilleur Prix" est à l'index final.
        
        // Supprimer les colonnes dynamiques existantes (celles entre Unité et Meilleur Prix)
        while (ComparisonGrid.Columns.Count > 4)
        {
            ComparisonGrid.Columns.RemoveAt(3);
        }

        // Ajouter les nouvelles colonnes pour chaque fournisseur sélectionné
        foreach (var quote in viewModel.SelectedSuppliers)
        {
            var column = new DataGridTextColumn
            {
                Header = quote.Supplier.CompanyName,
                Binding = new Binding($"Price_{quote.Id}"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                HeaderStyle = CreateCenteredHeaderStyle()
            };

            // Insérer avant la dernière colonne "Meilleur Prix"
            ComparisonGrid.Columns.Insert(ComparisonGrid.Columns.Count - 1, column);
        }
    }

    private Style CreateCenteredHeaderStyle()
    {
        var style = new Style(typeof(DataGridColumnHeader));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
        return style;
    }
}
