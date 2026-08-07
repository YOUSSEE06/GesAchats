using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GesAchats.Core.DTOs;

namespace GesAchats.WPF.Controls;

public partial class SituationOperationGroupControl : UserControl
{
    public static readonly DependencyProperty OperationGroupProperty =
        DependencyProperty.Register(
            nameof(OperationGroup),
            typeof(SituationOperationGroupDto),
            typeof(SituationOperationGroupControl),
            new PropertyMetadata(null, OnOperationGroupChanged));

    public SituationOperationGroupDto? OperationGroup
    {
        get => (SituationOperationGroupDto?)GetValue(OperationGroupProperty);
        set => SetValue(OperationGroupProperty, value);
    }

    public SituationOperationGroupControl()
    {
        InitializeComponent();
    }

    private static void OnOperationGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SituationOperationGroupControl control && e.NewValue is SituationOperationGroupDto group)
        {
            control.RenderGroup(group);
        }
    }

    private void RenderGroup(SituationOperationGroupDto group)
    {
        MainGrid.Children.Clear();
        MainGrid.RowDefinitions.Clear();

        // Add row definitions
        for (int i = 0; i < group.NombreSousLignes; i++)
        {
            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // --- BON DE COMMANDE ---
        // N° BC (merged)
        AddMergedCell(0, 0, group.NombreSousLignes, group.NumeroBC);
        // Date de commande (merged)
        if (group.DateCommande.HasValue)
        {
            AddMergedCell(1, 0, group.NombreSousLignes, group.DateCommande.Value.ToString("dd/MM/yyyy"));
        }
        // Total Commande HT (merged)
        AddMergedCell(7, 0, group.NombreSousLignes, group.TotalCommandeHT.ToString("N2"), isRightAligned: true);
        // Total Commande TTC (merged)
        AddMergedCell(8, 0, group.NombreSousLignes, group.TotalCommande.ToString("N2"), isRightAligned: true);

        // --- BON DE LIVRAISON ---
        if (group.HasDeliveryNote)
        {
            // N° BL (merged)
            AddMergedCell(9, 0, group.NombreSousLignes, group.NumeroBL);
            // Date de livraison (merged)
            if (group.DateLivraison.HasValue)
            {
                AddMergedCell(10, 0, group.NombreSousLignes, group.DateLivraison.Value.ToString("dd/MM/yyyy"));
            }
            // Etat BL (merged)
            AddMergedCell(15, 0, group.NombreSousLignes, group.BlEtat);
            // Total TTC / HT BL (merged)
            AddMergedCell(16, 0, group.NombreSousLignes, group.TotalBlTTC.ToString("N2"), isRightAligned: true);
            AddMergedCell(17, 0, group.NombreSousLignes, group.TotalBlHT.ToString("N2"), isRightAligned: true);
        }
        else
        {
            // No BL: merge entire BL section (columns 9-17)
            AddLargeMergedCell(9, 0, 9, group.NombreSousLignes, "Aucun bon de livraison");
        }

        // --- FACTURE ---
        if (group.HasInvoice)
        {
            // N° Facture (merged) — affiche le numéro fournisseur (fallback interne)
            AddMergedCell(18, 0, group.NombreSousLignes, group.NumeroFactureDisplay);
            // Date de facture (merged)
            if (group.DateFacture.HasValue)
            {
                AddMergedCell(19, 0, group.NombreSousLignes, group.DateFacture.Value.ToString("dd/MM/yyyy"));
            }
            // Total Facture (merged)
            AddMergedCell(24, 0, group.NombreSousLignes, group.TotalFacture.HasValue ? group.TotalFacture.Value.ToString("N2") : string.Empty, isRightAligned: true);
        }
        else
        {
            // No facture: merge entire Facture section (columns 18-24)
            AddLargeMergedCell(18, 0, 7, group.NombreSousLignes, "Aucune facture");
        }

        // --- RÈGLEMENTS ---
        if (group.HasInvoice && group.HasPayments)
        {
            // Total réglé (merged)
            AddMergedCell(29, 0, group.NombreSousLignes, group.TotalRegle.HasValue ? group.TotalRegle.Value.ToString("N2") : string.Empty, isRightAligned: true);
            // Reste à payer (merged)
            AddMergedCell(30, 0, group.NombreSousLignes, group.ResteAPayer.HasValue ? group.ResteAPayer.Value.ToString("N2") : string.Empty, isRightAligned: true);
            // Statut (merged)
            AddMergedCell(31, 0, group.NombreSousLignes, group.ReglementStatut);
        }
        else if (group.HasInvoice && !group.HasPayments)
        {
            // No payments but invoice exists: show "Aucun règlement" in columns 25-28, and keep totals
            AddLargeMergedCell(25, 0, 4, group.NombreSousLignes, "Aucun règlement");
            AddMergedCell(29, 0, group.NombreSousLignes, "0,00", isRightAligned: true);
            AddMergedCell(30, 0, group.NombreSousLignes, group.TotalFacture.HasValue ? group.TotalFacture.Value.ToString("N2") : string.Empty, isRightAligned: true);
            AddMergedCell(31, 0, group.NombreSousLignes, "En attente");
        }
        else
        {
            // No invoice: merge entire Règlements section
            AddLargeMergedCell(25, 0, 7, group.NombreSousLignes, "Aucun règlement");
        }

        // --- Add per-row cells ---
        for (int rowIndex = 0; rowIndex < group.SousLignes.Count; rowIndex++)
        {
            var sousLigne = group.SousLignes[rowIndex];
            var background = rowIndex % 2 == 1 ? new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)) : Brushes.White;

            // BC non-merged
            if (sousLigne.BcArticle != null)
            {
                AddCell(2, rowIndex, sousLigne.BcArticle, background);
                AddCell(3, rowIndex, sousLigne.BcPrixUnitaire.HasValue ? sousLigne.BcPrixUnitaire.Value.ToString("N2") : "—", background, isRightAligned: true);
                AddCell(4, rowIndex, sousLigne.BcQuantite.HasValue ? sousLigne.BcQuantite.Value.ToString("N0") : "—", background, isRightAligned: true);
                AddCell(5, rowIndex, sousLigne.BcTotalHT.HasValue ? sousLigne.BcTotalHT.Value.ToString("N2") : "—", background, isRightAligned: true);
                AddCell(6, rowIndex, sousLigne.BcTotal.HasValue ? sousLigne.BcTotal.Value.ToString("N2") : "—", background, isRightAligned: true);
            }

            // BL non-merged (only if BL exists)
            if (group.HasDeliveryNote && sousLigne.BlArticle != null)
            {
                AddCell(11, rowIndex, sousLigne.BlArticle, background);
                AddCell(12, rowIndex, sousLigne.BlQtBC.HasValue ? sousLigne.BlQtBC.Value.ToString("N0") : "—", background, isRightAligned: true);
                AddCell(13, rowIndex, sousLigne.BlQtLivree.HasValue ? sousLigne.BlQtLivree.Value.ToString("N0") : "—", background, isRightAligned: true);
                AddCell(14, rowIndex, sousLigne.BlEcart.HasValue ? sousLigne.BlEcart.Value.ToString("N0") : "—", background, isRightAligned: true);
            }

            // Facture non-merged (only if invoice exists)
            if (group.HasInvoice && sousLigne.FactureArticle != null)
            {
                AddCell(20, rowIndex, sousLigne.FactureArticle, background);
                AddCell(21, rowIndex, sousLigne.FactureMontantHT.HasValue ? sousLigne.FactureMontantHT.Value.ToString("N2") : "—", background, isRightAligned: true);
                AddCell(22, rowIndex, sousLigne.FactureTVA.HasValue ? sousLigne.FactureTVA.Value.ToString("N2") : "—", background, isRightAligned: true);
                AddCell(23, rowIndex, sousLigne.FactureMontantTTC.HasValue ? sousLigne.FactureMontantTTC.Value.ToString("N2") : "—", background, isRightAligned: true);
            }

            // Règlements non-merged (only if invoice and payments exist)
            if (group.HasInvoice && group.HasPayments && sousLigne.ReglementDate.HasValue)
            {
                AddCell(25, rowIndex, sousLigne.ReglementDate.Value.ToString("dd/MM/yyyy"), background);
                AddCell(26, rowIndex, sousLigne.ReglementMode ?? "—", background);
                AddCell(27, rowIndex, sousLigne.ReglementReference ?? "—", background);
                AddCell(28, rowIndex, sousLigne.ReglementMontant.HasValue ? sousLigne.ReglementMontant.Value.ToString("N2") : "—", background, isRightAligned: true);
            }
        }
    }

    private void AddMergedCell(int column, int row, int rowSpan, string text, bool isRightAligned = false)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.White
        };
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
            Padding = new Thickness(8, 6, 8, 6),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = isRightAligned ? TextAlignment.Right : TextAlignment.Left
        };
        border.Child = textBlock;
        Grid.SetColumn(border, column);
        Grid.SetRow(border, row);
        Grid.SetRowSpan(border, rowSpan);
        MainGrid.Children.Add(border);
    }

    private void AddLargeMergedCell(int startColumn, int startRow, int columnSpan, int rowSpan, string text)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.White
        };
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
            Padding = new Thickness(8, 6, 8, 6),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        border.Child = textBlock;
        Grid.SetColumn(border, startColumn);
        Grid.SetRow(border, startRow);
        Grid.SetColumnSpan(border, columnSpan);
        Grid.SetRowSpan(border, rowSpan);
        MainGrid.Children.Add(border);
    }

    private void AddCell(int column, int row, string text, Brush background, bool isRightAligned = false)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            BorderThickness = new Thickness(1),
            Background = background
        };
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
            Padding = new Thickness(8, 6, 8, 6),
            TextAlignment = isRightAligned ? TextAlignment.Right : TextAlignment.Left
        };
        border.Child = textBlock;
        Grid.SetColumn(border, column);
        Grid.SetRow(border, row);
        MainGrid.Children.Add(border);
    }
}
