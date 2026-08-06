using ClosedXML.Excel;
using GesAchats.Core.DTOs;

namespace GesAchats.WPF.Services;

public static class SuiviFournisseurExcelExporter
{
    // Palette (idem à la page XAML)
    private static readonly XLColor Purple = XLColor.FromHtml("#6C5CE7");
    private static readonly XLColor LightPurple = XLColor.FromHtml("#F1F0FA");
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#F8FAFC");
    private static readonly XLColor Border = XLColor.FromHtml("#E2E8F0");
    private static readonly XLColor TextDark = XLColor.FromHtml("#0F172A");
    private static readonly XLColor TextGray = XLColor.FromHtml("#64748B");
    private static readonly XLColor AltRow = XLColor.FromHtml("#F8FAFC");
    private static readonly XLColor Red = XLColor.FromHtml("#EF4444");
    private static readonly XLColor White = XLColor.White;

    public static void Export(string filePath, SituationFournisseurDto situation)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Situation");
        const int totalCols = 32;

        // ---------- Titre ----------
        ws.Cell(1, 1).Value = "Situation du Fournisseur";
        ws.Range(1, 1, 1, totalCols).Merge();
        ws.Range(1, 1, 1, totalCols).Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(TextDark);

        // ---------- Coordonnées fournisseur (verticales) ----------
        string[] infos =
        {
            $"Fournisseur : {situation.NomFournisseur}",
            $"Contact : {situation.NomContact ?? "—"}",
            $"Téléphone : {situation.Telephone ?? "—"}",
            $"Email : {situation.Email ?? "—"}",
            $"Ville : {situation.Ville ?? "—"}"
        };
        for (int i = 0; i < infos.Length; i++)
        {
            ws.Cell(2 + i, 1).Value = infos[i];
            ws.Range(2 + i, 1, 2 + i, totalCols).Merge();
            ws.Range(2 + i, 1, 2 + i, totalCols).Style.Font.SetFontSize(11).Font.SetFontColor(TextGray);
            ws.Range(2 + i, 1, 2 + i, totalCols).Style.Font.SetBold(i == 0);
            ws.Range(2 + i, 1, 2 + i, totalCols).Style.Font.SetFontColor(i == 0 ? Purple : TextGray);
        }

        ws.Cell(7, 1).Value = $"Généré le : {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Range(7, 1, 7, totalCols).Merge();
        ws.Range(7, 1, 7, totalCols).Style.Font.SetFontSize(9).Font.SetFontColor(TextGray);

        // ---------- Bandeau KPI ----------
        int kpiRow = 9;
        var kpis = new (string Label, string Value, bool Solde)[]
        {
            ("Total Commandes", situation.TotalCommandes.ToString(), false),
            ("Total Bons de Livraison", situation.TotalBls.ToString(), false),
            ("Total Factures", situation.TotalFactures.ToString(), false),
            ("Total Règlements", situation.TotalReglements.ToString("N2") + " MAD", false),
            ("SOLDE À PAYER", situation.SoldeAPayer.ToString("N2") + " MAD", true)
        };
        int[] segments = { 6, 6, 6, 7, 7 };
        int segCol = 1;
        for (int i = 0; i < kpis.Length; i++)
        {
            ws.Cell(kpiRow, segCol).Value = $"{kpis[i].Label} : {kpis[i].Value}";
            var rng = ws.Range(kpiRow, segCol, kpiRow, segCol + segments[i] - 1);
            if (segments[i] > 1) rng.Merge();
            rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rng.Style.Font.SetFontSize(11);
            if (kpis[i].Solde)
            {
                rng.Style.Fill.BackgroundColor = Purple;
                rng.Style.Font.SetFontColor(White);
            }
            else
            {
                rng.Style.Fill.BackgroundColor = HeaderBg;
                rng.Style.Font.SetFontColor(TextGray);
            }
            rng.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.TopBorderColor = Border;
            rng.Style.Border.BottomBorderColor = Border;
            rng.Style.Border.LeftBorderColor = Border;
            rng.Style.Border.RightBorderColor = Border;
            rng.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.InsideBorderColor = Border;
            segCol += segments[i];
        }

        // ---------- En-têtes ----------
        int sectionRow = kpiRow + 2;
        int headerRow = sectionRow + 1;

        ws.Range(sectionRow, 1, sectionRow, 9).Merge();
        ws.Range(sectionRow, 10, sectionRow, 18).Merge();
        ws.Range(sectionRow, 19, sectionRow, 25).Merge();
        ws.Range(sectionRow, 26, sectionRow, 32).Merge();
        ApplySectionHeader(ws.Range(sectionRow, 1, sectionRow, 9), "BON DE COMMANDE",
            XLColor.FromHtml("#E2EFDA"), XLColor.FromHtml("#538135"));
        ApplySectionHeader(ws.Range(sectionRow, 10, sectionRow, 18), "BON DE LIVRAISON",
            XLColor.FromHtml("#E4DFEC"), XLColor.FromHtml("#7030A0"));
        ApplySectionHeader(ws.Range(sectionRow, 19, sectionRow, 25), "FACTURE",
            XLColor.FromHtml("#FFF2CC"), XLColor.FromHtml("#BF8F00"));
        ApplySectionHeader(ws.Range(sectionRow, 26, sectionRow, 32), "RÈGLEMENTS",
            XLColor.FromHtml("#FCE4D6"), XLColor.FromHtml("#C55A11"));

        string[] headers =
        {
            "N° BC", "Date de commande", "Désignation", "Prix Unitaire", "Quantité",
            "Total HT", "Total TTC", "Total de commande HT", "Total de commande TTC",
            "N° BL", "Date de livraison", "Désignation", "Qté Cmd", "Qté Livrée",
            "Écart", "État", "Total de commande TTC", "Total de commande HT",
            "N° Facture", "Date de facture", "Désignation", "Montant HT", "TVA",
            "Montant TTC", "Total Facture",
            "Date", "Mode", "Référence", "Montant", "Total Réglé",
            "Reste à Payer", "Statut"
        };
        for (int c = 0; c < headers.Length; c++)
        {
            ApplyColumnHeader(ws.Cell(headerRow, c + 1), headers[c]);
        }
        ws.Range(headerRow, 1, headerRow, totalCols).Style.Alignment.WrapText = true;
        ws.Row(headerRow).Height = 32;

        // ---------- Données ----------
        int row = headerRow + 1;
        bool zebra = false;

        foreach (var op in situation.Operations)
        {
            int n = Math.Max(op.NombreSousLignes, 1);
            int start = row;
            int end = row + n - 1;

            // BC (cellules fusionnées)
            WriteMergedText(ws, start, end, 1, op.NumeroBC);
            WriteMergedText(ws, start, end, 2, op.DateCommande?.ToString("dd/MM/yyyy") ?? string.Empty);
            WriteMergedNumber(ws, start, end, 8, op.TotalCommandeHT);
            WriteMergedNumber(ws, start, end, 9, op.TotalCommande);

            // BL
            if (op.HasDeliveryNote)
            {
                WriteMergedText(ws, start, end, 10, op.NumeroBL);
                WriteMergedText(ws, start, end, 11, op.DateLivraison?.ToString("dd/MM/yyyy") ?? string.Empty);
                WriteMergedText(ws, start, end, 16, op.BlEtat);
                WriteMergedNumber(ws, start, end, 17, op.TotalBlTTC);
                WriteMergedNumber(ws, start, end, 18, op.TotalBlHT);
            }
            else
            {
                WriteMergedPlaceholder(ws, start, end, 10, 18, "Aucun bon de livraison");
            }

            // Facture
            if (op.HasInvoice)
            {
                WriteMergedText(ws, start, end, 19, op.NumeroFacture);
                WriteMergedText(ws, start, end, 20, op.DateFacture?.ToString("dd/MM/yyyy") ?? string.Empty);
                WriteMergedNumber(ws, start, end, 25, op.TotalFacture ?? 0m);
            }
            else
            {
                WriteMergedPlaceholder(ws, start, end, 19, 25, "Aucune facture");
            }

            // Règlements
            if (op.HasInvoice && op.HasPayments)
            {
                WriteMergedNumber(ws, start, end, 30, op.TotalRegle ?? 0m);
                WriteMergedNumber(ws, start, end, 31, op.ResteAPayer ?? 0m);
                WriteMergedText(ws, start, end, 32, op.ReglementStatut);
            }
            else if (op.HasInvoice)
            {
                WriteMergedPlaceholder(ws, start, end, 26, 29, "Aucun règlement");
                WriteMergedText(ws, start, end, 30, "0,00");
                WriteMergedNumber(ws, start, end, 31, op.TotalFacture ?? 0m);
                WriteMergedText(ws, start, end, 32, "En attente");
            }
            else
            {
                WriteMergedPlaceholder(ws, start, end, 26, 32, "Aucun règlement");
            }

            // Lignes de détail
            for (int i = 0; i < n; i++)
            {
                int r = start + i;
                var bg = zebra ? AltRow : White;
                zebra = !zebra;
                if (i < op.SousLignes.Count)
                {
                    var sl = op.SousLignes[i];
                    if (sl.BcArticle != null)
                    {
                        WriteText(ws, r, 3, sl.BcArticle, bg);
                        WriteNumber(ws, r, 4, sl.BcPrixUnitaire, bg);
                        WriteNumber(ws, r, 5, sl.BcQuantite, bg, "#,##0");
                        WriteNumber(ws, r, 6, sl.BcTotalHT, bg);
                        WriteNumber(ws, r, 7, sl.BcTotal, bg);
                    }
                    if (op.HasDeliveryNote && sl.BlArticle != null)
                    {
                        WriteText(ws, r, 12, sl.BlArticle, bg);
                        WriteNumber(ws, r, 13, sl.BlQtBC, bg, "#,##0");
                        WriteNumber(ws, r, 14, sl.BlQtLivree, bg, "#,##0");
                        WriteNumber(ws, r, 15, sl.BlEcart, bg, "#,##0");
                    }
                    if (op.HasInvoice && sl.FactureArticle != null)
                    {
                        WriteText(ws, r, 21, sl.FactureArticle, bg);
                        WriteNumber(ws, r, 22, sl.FactureMontantHT, bg);
                        WriteNumber(ws, r, 23, sl.FactureTVA, bg);
                        WriteNumber(ws, r, 24, sl.FactureMontantTTC, bg);
                    }
                    if (op.HasInvoice && op.HasPayments && sl.ReglementDate.HasValue)
                    {
                        ws.Cell(r, 26).Value = sl.ReglementDate.Value;
                        ws.Cell(r, 26).Style.DateFormat.Format = "dd/MM/yyyy";
                        ApplyCellBg(ws.Cell(r, 26), bg);
                        WriteText(ws, r, 27, sl.ReglementMode, bg);
                        WriteText(ws, r, 28, sl.ReglementReference, bg);
                        WriteNumber(ws, r, 29, sl.ReglementMontant, bg);
                    }
                }
                else
                {
                    ZebraRow(ws, r, bg);
                }
            }

            ws.Range(start, 1, end, totalCols).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(start, 1, end, totalCols).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(start, 1, end, totalCols).Style.Border.TopBorderColor = Border;
            ws.Range(start, 1, end, totalCols).Style.Border.BottomBorderColor = Border;
            row = end + 1;
        }

        // ---------- Ligne des totaux ----------
        int totalRow = row + 1;
        ws.Cell(totalRow, 1).Value = "TOTAUX";
        ws.Range(totalRow, 1, totalRow, totalCols).Merge();
        ws.Range(totalRow, 1, totalRow, totalCols).Style.Fill.BackgroundColor = LightPurple;
        ws.Range(totalRow, 1, totalRow, totalCols).Style.Font.SetBold().Font.SetFontColor(Purple).Font.SetFontSize(11);
        ws.Range(totalRow, 1, totalRow, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(totalRow).Height = 24;

        var ops = situation.Operations;
        decimal totalCmdHT = ops.Sum(o => o.TotalCommandeHT);
        decimal totalCmdTTC = ops.Sum(o => o.TotalCommande);
        decimal totalBlTTC = ops.Where(o => o.HasDeliveryNote).Sum(o => o.TotalBlTTC);
        decimal totalBlHT = ops.Where(o => o.HasDeliveryNote).Sum(o => o.TotalBlHT);
        decimal totalFacture = ops.Where(o => o.HasInvoice && o.TotalFacture.HasValue).Sum(o => o.TotalFacture!.Value);
        decimal totalPayments = ops.Where(o => o.HasPayments).SelectMany(o => o.Reglements).Sum(r => r.Montant);
        decimal totalRegle = ops.Where(o => o.HasInvoice).Sum(o => o.TotalRegle ?? 0m);
        decimal totalReste = ops.Where(o => o.HasInvoice).Sum(o => o.ResteAPayer ?? 0m);

        WriteTotal(ws, totalRow, 8, totalCmdHT);
        WriteTotal(ws, totalRow, 9, totalCmdTTC);
        WriteTotal(ws, totalRow, 17, totalBlTTC);
        WriteTotal(ws, totalRow, 18, totalBlHT);
        WriteTotal(ws, totalRow, 25, totalFacture);
        WriteTotal(ws, totalRow, 29, totalPayments);
        WriteTotal(ws, totalRow, 30, totalRegle);
        WriteTotal(ws, totalRow, 31, totalReste, isRed: totalReste != 0);

        // ---------- Largeurs ----------
        var widths = new double[] { 16, 18, 32, 14, 10, 13, 13, 18, 20, 14, 14, 32, 10, 10, 10, 16, 18, 18, 18, 14, 32, 16, 12, 16, 18, 13, 18, 15, 13, 14, 14, 15 };
        for (int c = 0; c < widths.Length; c++)
        {
            ws.Column(c + 1).Width = widths[c];
        }

        // Mise en page : pleine largeur / paysage
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = 0.3;
        ws.PageSetup.Margins.Right = 0.3;

        ws.Range(1, 1, totalRow, totalCols).Style.Font.FontName = "Arial";
        ws.Range(1, 1, headerRow, totalCols).Style.Font.FontName = "Arial";
        ws.SheetView.FreezeRows(headerRow);

        wb.SaveAs(filePath);
    }

    private static void ApplySectionHeader(IXLRange range, string title, XLColor fill, XLColor text)
    {
        range.Value = title;
        range.Style.Fill.BackgroundColor = fill;
        range.Style.Font.SetBold().Font.SetFontColor(text).Font.SetFontSize(11);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorderColor = Border;
        range.Style.Border.OutsideBorderColor = Border;
    }

    private static void ApplyColumnHeader(IXLCell cell, string title)
    {
        cell.Value = title;
        cell.Style.Fill.BackgroundColor = HeaderBg;
        cell.Style.Font.SetBold().Font.SetFontSize(10).Font.SetFontColor(TextGray);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.TopBorderColor = Border;
        cell.Style.Border.BottomBorderColor = Border;
        cell.Style.Border.LeftBorderColor = Border;
        cell.Style.Border.RightBorderColor = Border;
    }

    private static void WriteMergedText(IXLWorksheet ws, int startRow, int endRow, int col, string text)
    {
        var rng = ws.Range(startRow, col, endRow, col);
        if (endRow > startRow) rng.Merge();
        var cell = rng.FirstCell();
        cell.Value = text;
        cell.Style.Font.SetFontSize(10).Font.SetFontColor(TextDark);
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorderColor = Border;
        cell.Style.Border.RightBorderColor = Border;
    }

    private static void WriteMergedNumber(IXLWorksheet ws, int startRow, int endRow, int col, decimal value)
    {
        var rng = ws.Range(startRow, col, endRow, col);
        if (endRow > startRow) rng.Merge();
        var cell = rng.FirstCell();
        cell.Value = (double)value;
        cell.Style.NumberFormat.Format = "#,##0.00";
        cell.Style.Font.SetFontSize(10).Font.SetFontColor(TextDark);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorderColor = Border;
        cell.Style.Border.RightBorderColor = Border;
    }

    private static void WriteMergedPlaceholder(IXLWorksheet ws, int startRow, int endRow, int startCol, int endCol, string text)
    {
        var rng = ws.Range(startRow, startCol, endRow, endCol);
        rng.Merge();
        var cell = rng.FirstCell();
        cell.Value = text;
        cell.Style.Font.SetFontSize(10);
        cell.Style.Font.SetFontColor(TextGray);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.InsideBorderColor = Border;
        cell.Style.Border.OutsideBorderColor = Border;
    }

    private static void WriteText(IXLWorksheet ws, int row, int col, string text, XLColor bg)
    {
        var cell = ws.Cell(row, col);
        cell.Value = text;
        cell.Style.Font.SetFontSize(10).Font.SetFontColor(TextDark);
        ApplyCellBg(cell, bg);
    }

    private static void WriteNumber(IXLWorksheet ws, int row, int col, decimal? value, XLColor bg, string fmt = "#,##0.00")
    {
        var cell = ws.Cell(row, col);
        cell.Value = (double)(value ?? 0m);
        cell.Style.NumberFormat.Format = fmt;
        cell.Style.Font.SetFontSize(10).Font.SetFontColor(TextDark);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ApplyCellBg(cell, bg);
    }

    private static void ApplyCellBg(IXLCell cell, XLColor bg)
    {
        cell.Style.Fill.BackgroundColor = bg;
        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.TopBorderColor = Border;
        cell.Style.Border.BottomBorderColor = Border;
        cell.Style.Border.LeftBorderColor = Border;
        cell.Style.Border.RightBorderColor = Border;
    }

    private static void ZebraRow(IXLWorksheet ws, int row, XLColor bg)
    {
        const int totalCols = 32;
        var rng = ws.Range(row, 1, row, totalCols);
        rng.Style.Fill.BackgroundColor = bg;
    }

    private static void WriteTotal(IXLWorksheet ws, int row, int col, decimal value, bool isRed = false)
    {
        var cell = ws.Cell(row, col);
        cell.Value = (double)value;
        cell.Style.NumberFormat.Format = "#,##0.00";
        cell.Style.Font.SetBold();
        cell.Style.Font.SetFontColor(isRed ? Red : Purple);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorderColor = Border;
        cell.Style.Border.RightBorderColor = Border;
    }
}