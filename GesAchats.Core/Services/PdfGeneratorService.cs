using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace GesAchats.Core.Services;

public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly string _outputPath;

    public PdfGeneratorService()
    {
        // QuestPDF License configuration
        QuestPDF.Settings.License = LicenseType.Community;
        _outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
        if (!Directory.Exists(_outputPath))
            Directory.CreateDirectory(_outputPath);
    }

    public async Task<string> GenerateDeliveryNotePdfAsync(DeliveryNote bl)
    {
        try
        {
            // 1. Générer le HTML
            string htmlContent = GenerateBlHtmlContent(bl);

            // 2. Convertir en PDF avec wkhtmltopdf
            string fileName = $"BL_{bl.DeliveryNumber.Replace("-", "_")}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            // Sauvegarder HTML temporaire
            string htmlPath = Path.Combine(_outputPath, $"BL_{bl.DeliveryNumber}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erreur conversion PDF wkhtmltopdf (code {process.ExitCode}). Assurez-vous que wkhtmltopdf est installé.");
                    }
                }
            }

            // Nettoyer HTML temporaire
            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF BL: {ex.Message}", ex);
        }
    }

    private string GenerateBlHtmlContent(DeliveryNote bl)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine($"    <title>Bon de Livraison - {bl.DeliveryNumber}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; color: #333; }");
        html.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 30px; }");
        html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 2px solid #1a3a52; padding-bottom: 10px; }");
        html.AppendLine("        .header-left h1 { color: #1a3a52; font-size: 24px; }");
        html.AppendLine("        .header-right { text-align: right; }");
        html.AppendLine("        .header-right h2 { color: #ccc; font-size: 32px; margin-bottom: 5px; }");
        html.AppendLine("        .boxes { display: flex; gap: 20px; margin-bottom: 30px; margin-top: 20px; }");
        html.AppendLine("        .box { flex: 1; border: 1px solid #1a3a52; padding: 15px; border-radius: 4px; }");
        html.AppendLine("        .box h3 { color: #1a3a52; margin-bottom: 8px; font-size: 14px; border-bottom: 1px solid #1a3a52; padding-bottom: 5px; }");
        html.AppendLine("        .section-title { background: #1a3a52; color: white; padding: 8px 15px; margin: 20px 0 10px 0; font-weight: bold; font-size: 14px; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
        html.AppendLine("        th, td { border: 1px solid #ddd; padding: 10px; text-align: left; font-size: 12px; }");
        html.AppendLine("        th { background: #f2f7fb; color: #1a3a52; font-weight: bold; }");
        html.AppendLine("        .conformite-ok { background-color: #d4edda; color: #155724; }");
        html.AppendLine("        .conformite-warning { background-color: #fff3cd; color: #856404; }");
        html.AppendLine("        .conformite-error { background-color: #f8d7da; color: #721c24; }");
        html.AppendLine("        .scan-placeholder { border: 2px dashed #bbb; padding: 30px; text-align: center; color: #999; margin: 15px 0; min-height: 100px; }");
        html.AppendLine("        .observations { background: #f9f9f9; border: 1px solid #ddd; padding: 15px; margin: 15px 0; border-radius: 4px; font-size: 12px; }");
        html.AppendLine("        .signatures { display: flex; gap: 40px; margin: 30px 0; }");
        html.AppendLine("        .signature-block { flex: 1; text-align: center; border: 1px solid #ddd; padding: 15px; border-radius: 4px; }");
        html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 40px; border-top: 1px solid #eee; padding-top: 10px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        html.AppendLine("    <div class=\"container\">");
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"header-left\">");
        html.AppendLine("                <h1>GesAchats v2.0</h1>");
        html.AppendLine("                <p>Module Magasinier</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"header-right\">");
        html.AppendLine("                <h2>BON DE LIVRAISON</h2>");
        html.AppendLine($"                <p><strong>N°: {bl.DeliveryNumber}</strong></p>");
        html.AppendLine($"                <p>Date Réception: {bl.ReceptionDate:dd/MM/yyyy}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"boxes\">");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>FOURNISSEUR</h3>");
        html.AppendLine($"                <p><strong>{bl.Supplier?.CompanyName ?? "N/A"}</strong><br>");
        html.AppendLine($"                {bl.Supplier?.Address ?? ""}<br>");
        html.AppendLine($"                {bl.Supplier?.City ?? ""}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>RÉFÉRENCE BC</h3>");
        html.AppendLine($"                <p><strong>{bl.PurchaseOrder?.OrderNumber ?? "N/A"}</strong><br>");
        html.AppendLine($"                Date BC: {bl.PurchaseOrder?.OrderDate:dd/MM/yyyy}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>RÉCEPTION</h3>");
        html.AppendLine($"                <p>Reçu par: {bl.ReceivedBy?.FullName ?? "Magasinier"}</p>");
        html.AppendLine($"                <p>Statut: {bl.Status}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"section-title\">RÉCAPITULATIF DES ARTICLES</div>");
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>Quantité Reçue</th>");
        html.AppendLine("                    <th>Quantité Conforme</th>");
        html.AppendLine("                    <th>Quantité Défectueuse</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");
        html.AppendLine("                <tr>");
        html.AppendLine($"                    <td style=\"text-align: center; font-size: 16px;\">{bl.ReceivedQuantity}</td>");
        html.AppendLine($"                    <td style=\"text-align: center; font-size: 16px;\" class=\"conformite-ok\">{bl.CompliantQuantity}</td>");
        html.AppendLine($"                    <td style=\"text-align: center; font-size: 16px;\" class=\"conformite-error\">{bl.DefectiveQuantity}</td>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");

        html.AppendLine("        <div class=\"section-title\">DOCUMENT ORIGINAL</div>");
        html.AppendLine("        <div class=\"scan-placeholder\">");
        if (!string.IsNullOrEmpty(bl.FilePath) && File.Exists(bl.FilePath))
        {
            html.AppendLine($"            <img src=\"file:///{bl.FilePath}\" style=\"max-width: 100%; max-height: 400px;\" />");
        }
        else
        {
            html.AppendLine("            [Scan ou photo du Bon de Livraison original du fournisseur]");
        }
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"section-title\">OBSERVATIONS / NOTES</div>");
        html.AppendLine("        <div class=\"observations\">");
        html.AppendLine($"            <p>{(string.IsNullOrEmpty(bl.Observations) ? "Aucune observation particulière." : bl.Observations)}</p>");
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"section-title\">VALIDATIONS</div>");
        html.AppendLine("        <div class=\"signatures\">");
        html.AppendLine("            <div class=\"signature-block\">");
        html.AppendLine("                <p><strong>Le Magasinier:</strong></p>");
        html.AppendLine($"                <p style=\"margin-top: 40px;\">{bl.ReceivedBy?.FullName ?? "..........................."}</p>");
        html.AppendLine("                <p style=\"margin-top: 20px; border-top: 1px solid #333; padding-top: 5px; font-size: 10px;\">Signature & Date</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"signature-block\">");
        html.AppendLine("                <p><strong>Le Transporteur / Fournisseur:</strong></p>");
        html.AppendLine($"                <p style=\"margin-top: 40px;\">{bl.Supplier?.CompanyName ?? "..........................."}</p>");
        html.AppendLine("                <p style=\"margin-top: 20px; border-top: 1px solid #333; padding-top: 5px; font-size: 10px;\">Signature & Date</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Document généré par GesAchats v2.0 - Module Gestion des Stocks - Confidentiel</p>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GeneratePaymentReceiptPdfAsync(Payment reglement)
    {
        try
        {
            // 1. Générer le HTML
            string htmlContent = GeneratePaymentReceiptHtmlContent(reglement);

            // 2. Convertir en PDF
            string fileName = $"RECU_{reglement.PaymentNumber.Replace("-", "_")}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            string htmlPath = Path.Combine(_outputPath, $"RECU_{reglement.PaymentNumber}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }

            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF Reçu: {ex.Message}", ex);
        }
    }

    private string GeneratePaymentReceiptHtmlContent(Payment reglement)
    {
        StringBuilder html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine($"    <title>Reçu de Paiement - {reglement.PaymentNumber}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 40px; }");
        html.AppendLine("        .header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 30px; }");
        html.AppendLine("        .content { line-height: 1.6; }");
        html.AppendLine("        .amount { font-size: 24px; font-weight: bold; margin: 20px 0; }");
        html.AppendLine("        .footer { margin-top: 50px; font-size: 12px; color: #777; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class='header'>");
        html.AppendLine("        <h1>REÇU DE PAIEMENT</h1>");
        html.AppendLine($"        <p>Référence : {reglement.PaymentNumber}</p>");
        html.AppendLine("    </div>");
        html.AppendLine("    <div class='content'>");
        html.AppendLine($"        <p>Date de paiement : {reglement.PaymentDate:dd/MM/yyyy}</p>");
        html.AppendLine($"        <p>Fournisseur : {reglement.Supplier?.CompanyName}</p>");
        html.AppendLine($"        <p>Facture associée : {reglement.Invoice?.InvoiceNumber}</p>");
        html.AppendLine($"        <p>Mode de paiement : {reglement.PaymentMethod}</p>");
        html.AppendLine($"        <div class='amount'>Montant réglé : {reglement.AmountPaid:N2} MAD</div>");
        if (!string.IsNullOrEmpty(reglement.ReferenceNumber))
            html.AppendLine($"        <p>Référence transaction : {reglement.ReferenceNumber}</p>");
        html.AppendLine("    </div>");
        html.AppendLine("    <div class='footer'>");
        html.AppendLine($"        <p>Généré le {DateTime.Now:dd/MM/yyyy HH:mm} - GesAchats v2.0</p>");
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    public async Task<string> GeneratePurchaseOrderPdfAsync(PurchaseOrder bc)
    {
        try
        {
            // 1. Générer le HTML
            string htmlContent = GenerateBcHtmlContent(bc);

            // 2. Convertir en PDF avec wkhtmltopdf
            string fileName = $"BC_{bc.OrderNumber.Replace("-", "_")}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            // Sauvegarder HTML temporaire
            string htmlPath = Path.Combine(_outputPath, $"BC_{bc.OrderNumber}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erreur conversion PDF wkhtmltopdf (code {process.ExitCode}). Assurez-vous que wkhtmltopdf est installé.");
                    }
                }
            }

            // Nettoyer HTML temporaire
            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF BC: {ex.Message}", ex);
        }
    }

    private string GenerateBcHtmlContent(PurchaseOrder bc)
    { 
        StringBuilder html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine($"    <title>Bon de Commande - {bc.OrderNumber}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; color: #333; }");
        html.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
        html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #1a3a52; padding-bottom: 20px; }");
        html.AppendLine("        .header-left h1 { color: #0066cc; font-size: 28px; margin-bottom: 5px; }");
        html.AppendLine("        .header-left p { color: #666; font-size: 14px; }");
        html.AppendLine("        .header-right { text-align: right; }");
        html.AppendLine("        .header-right h2 { color: #ccc; font-size: 48px; font-weight: bold; margin-bottom: 10px; }");
        html.AppendLine("        .header-right p { font-size: 14px; margin: 5px 0; }");
        html.AppendLine("        .bc-number { font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        .section-title { background-color: #1a3a52; color: white; padding: 10px 15px; margin: 20px 0 10px 0; font-size: 13px; font-weight: bold; }");
        html.AppendLine("        .boxes { display: flex; gap: 20px; margin-bottom: 30px; flex-wrap: wrap; }");
        html.AppendLine("        .box { flex: 1; min-width: 250px; border: 2px solid #1a3a52; padding: 15px; border-radius: 4px; }");
        html.AppendLine("        .box h3 { color: #1a3a52; font-size: 13px; font-weight: bold; margin-bottom: 8px; }");
        html.AppendLine("        .box p { font-size: 12px; line-height: 1.6; color: #555; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th { background: #e6f0fa; border: 1px solid #bbb; padding: 10px; text-align: left; font-size: 12px; font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        td { border: 1px solid #bbb; padding: 10px; font-size: 12px; }");
        html.AppendLine("        tbody tr:nth-child(even) { background: #f9f9f9; }");
        html.AppendLine("        .text-right { text-align: right; }");
        html.AppendLine("        .text-center { text-align: center; }");
        html.AppendLine("        .resume-financier { text-align: right; margin: 30px 0; }");
        html.AppendLine("        .resume-table { display: inline-block; border: 1px solid #bbb; border-collapse: collapse; }");
        html.AppendLine("        .resume-row { display: flex; border-bottom: 1px solid #bbb; }");
        html.AppendLine("        .resume-label { text-align: left; font-weight: bold; background-color: #f0f0f0; padding: 10px 20px; min-width: 150px; border-right: 1px solid #bbb; }");
        html.AppendLine("        .resume-value { text-align: right; padding: 10px 20px; min-width: 100px; font-size: 13px; }");
        html.AppendLine("        .total-ttc { background-color: #1a3a52; color: white; font-weight: bold; }");
        html.AppendLine("        .total-ttc .resume-value { color: #0066cc; font-size: 16px; }");
        html.AppendLine("        .conditions { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin: 20px 0; font-size: 12px; }");
        html.AppendLine("        .condition-item { background-color: #f9f9f9; padding: 10px; border-left: 3px solid #0066cc; }");
        html.AppendLine("        .condition-label { font-weight: bold; color: #1a3a52; margin-bottom: 3px; }");
        html.AppendLine("        .observations { background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; padding: 15px; margin: 20px 0; font-size: 12px; line-height: 1.6; }");
        html.AppendLine("        .signatures { display: flex; justify-content: space-between; margin: 40px 0 20px 0; font-size: 11px; }");
        html.AppendLine("        .signature-block { flex: 1; margin: 0 10px; text-align: center; }");
        html.AppendLine("        .signature-line { margin-top: 30px; border-top: 1px solid #333; padding-top: 10px; }");
        html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        
        // En-tête
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"header-left\">");
        html.AppendLine("                <h1>GesAchats v2.0</h1>");
        html.AppendLine("                <p>Module Responsable d'Achat</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"header-right\">");
        html.AppendLine("                <h2>BON DE<br>COMMANDE</h2>");
        html.AppendLine($"                <p><span class=\"bc-number\">N°: {bc.OrderNumber}</span></p>");
        html.AppendLine($"                <p>Date: {bc.OrderDate:dd/MM/yyyy}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Boîtes info
        html.AppendLine("        <div class=\"boxes\">");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>FOURNISSEUR</h3>");
        html.AppendLine($"                <p><strong>{bc.Supplier?.CompanyName ?? "N/A"}</strong><br>");
        html.AppendLine($"                {bc.Supplier?.Address ?? ""}<br>");
        html.AppendLine($"                {bc.Supplier?.City ?? ""}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>EXPÉDIÉ À</h3>");
        html.AppendLine($"                <p><strong>GesAchats - Siège Social</strong><br>");
        html.AppendLine($"                123 Rue de l'Industrie<br>");
        html.AppendLine($"                Casablanca, Maroc</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"box\">");
        html.AppendLine("                <h3>INFORMATIONS</h3>");
        html.AppendLine($"                <p><strong>Référence Besoin:</strong> BES-{bc.NeedId ?? 0}<br>");
        html.AppendLine($"                <strong>Conditions Paiement:</strong> {bc.PaymentConditions ?? "Net 30 jours"}<br>");
        html.AppendLine($"                <strong>Délai Livraison:</strong> {bc.RequestedDeliveryDelay ?? 5} jours ouvrables</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Tableau articles
        html.AppendLine("        <div class=\"section-title\">ARTICLES COMMANDÉS</div>");
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>#</th>");
        html.AppendLine("                    <th>Désignation</th>");
        html.AppendLine("                    <th class=\"text-center\">Quantité</th>");
        html.AppendLine("                    <th>Unité</th>");
        html.AppendLine("                    <th class=\"text-right\">PU HT</th>");
        html.AppendLine("                    <th class=\"text-right\">PU TTC</th>");
        html.AppendLine("                    <th class=\"text-right\">Total TTC</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");

        int i = 1;
        foreach (var detail in bc.Details)
        { 
            html.AppendLine("                <tr>");
            html.AppendLine($"                    <td>{i++}</td>");
            html.AppendLine($"                    <td>{detail.Product?.Designation ?? "N/A"}</td>");
            html.AppendLine($"                    <td class=\"text-center\">{detail.Quantity:N2}</td>");
            html.AppendLine($"                    <td>{detail.Product?.Unit ?? "Unité"}</td>");
            html.AppendLine($"                    <td class=\"text-right\">{detail.UnitPriceHT:N2} MAD</td>");
            html.AppendLine($"                    <td class=\"text-right\">{detail.UnitPriceTTC:N2} MAD</td>");
            html.AppendLine($"                    <td class=\"text-right\"><strong>{(detail.Quantity * detail.UnitPriceTTC):N2} MAD</strong></td>");
            html.AppendLine("                </tr>");
        }

        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");

        // Résumé financier
        html.AppendLine("        <div class=\"resume-financier\">");
        html.AppendLine("            <div class=\"resume-table\">");
        html.AppendLine("                <div class=\"resume-row\">");
        html.AppendLine("                    <div class=\"resume-label\">Total HT</div>");
        html.AppendLine($"                    <div class=\"resume-value\">{bc.TotalAmountHT:N2} MAD</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"resume-row\">");
        var vatRate = bc.TotalAmountHT > 0 ? (bc.TotalVAT / bc.TotalAmountHT * 100) : 0;
        html.AppendLine($"                    <div class=\"resume-label\">TVA ({vatRate:N2}%)</div>");
        html.AppendLine($"                    <div class=\"resume-value\">{bc.TotalVAT:N2} MAD</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"resume-row total-ttc\">");
        html.AppendLine("                    <div class=\"resume-label\">TOTAL TTC</div>");
        html.AppendLine($"                    <div class=\"resume-value\">{bc.TotalAmountTTC:N2} MAD</div>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Conditions
        html.AppendLine("        <div class=\"section-title\">CONDITIONS COMMERCIALES</div>");
        html.AppendLine("        <div class=\"conditions\">");
        html.AppendLine("            <div class=\"condition-item\">");
        html.AppendLine("                <div class=\"condition-label\">Mode de Paiement</div>");
        html.AppendLine("                <div class=\"condition-value\">Virement bancaire</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"condition-item\">");
        html.AppendLine("                <div class=\"condition-label\">Conditions Paiement</div>");
        html.AppendLine($"                <div class=\"condition-value\">{bc.PaymentConditions ?? "Net 30 jours"}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"condition-item\">");
        html.AppendLine("                <div class=\"condition-label\">Délai de Livraison</div>");
        html.AppendLine($"                <div class=\"condition-value\">{bc.RequestedDeliveryDelay ?? 5} jours ouvrables</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"condition-item\">");
        html.AppendLine("                <div class=\"condition-label\">Lieu de Livraison</div>");
        html.AppendLine("                <div class=\"condition-value\">Casablanca, Maroc</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Observations
        if (!string.IsNullOrEmpty(bc.Observations))
        { 
            html.AppendLine("        <div class=\"section-title\">OBSERVATIONS</div>");
            html.AppendLine("        <div class=\"observations\">");
            html.AppendLine($"            <p>{bc.Observations}</p>");
            html.AppendLine("        </div>");
        }

        // Signatures
        html.AppendLine("        <div class=\"section-title\">VALIDATIONS</div>");
        html.AppendLine("        <div class=\"signatures\">");
        html.AppendLine("            <div class=\"signature-block\">");
        html.AppendLine("                <p><strong>Créé par:</strong></p>");
        html.AppendLine($"                <p style=\"margin-top: 20px; font-size: 12px;\">{bc.CreatedBy?.FullName ?? "Responsable Achats"}</p>");
        html.AppendLine("                <div class=\"signature-line\">");
        html.AppendLine($"                    <p>Date: {bc.OrderDate:dd/MM/yyyy}</p>");
        html.AppendLine("                    <p>Signature: ___________</p>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"signature-block\">");
        html.AppendLine("                <p><strong>Approuvé par:</strong></p>");
        html.AppendLine("                <p style=\"margin-top: 20px; font-size: 12px;\">Directeur Administratif</p>");
        html.AppendLine("                <div class=\"signature-line\">");
        html.AppendLine("                    <p>Date: ___________</p>");
        html.AppendLine("                    <p>Signature: ___________</p>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Document généré par GesAchats v2.0 - Système de Gestion des Achats</p>");
        html.AppendLine("            <p>Confidentiel - Usage interne uniquement</p>");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateNeedsListPdfAsync(IEnumerable<Need> needs)
    {
        try
        {
            var needList = needs.ToList();
            if (!needList.Any())
                throw new Exception("La liste des besoins est vide.");

            string htmlContent = GenerateNeedsListHtmlContent(needList);

            string fileName = $"BESOINS_{DateTime.Now:yyyyMMdd}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            string htmlPath = Path.Combine(_outputPath, $"BESOINS_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erreur conversion PDF wkhtmltopdf (code {process.ExitCode}). Assurez-vous que wkhtmltopdf est installé.");
                    }
                }
            }

            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF Liste des Besoins: {ex.Message}", ex);
        }
    }

    private string GenerateNeedsListHtmlContent(List<Need> needs)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <title>Liste des Besoins</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; color: #333; }");
        html.AppendLine("        .container { max-width: 1100px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
        html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #1a3a52; padding-bottom: 20px; }");
        html.AppendLine("        .header-left h1 { color: #0066cc; font-size: 28px; margin-bottom: 5px; }");
        html.AppendLine("        .header-left p { color: #666; font-size: 14px; }");
        html.AppendLine("        .header-right { text-align: right; }");
        html.AppendLine("        .header-right h2 { color: #ccc; font-size: 36px; font-weight: bold; margin-bottom: 10px; }");
        html.AppendLine("        .header-right p { font-size: 14px; margin: 5px 0; }");
        html.AppendLine("        .generation-date { font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        .section-title { background-color: #1a3a52; color: white; padding: 10px 15px; margin: 20px 0 10px 0; font-size: 13px; font-weight: bold; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th { background: #e6f0fa; border: 1px solid #bbb; padding: 10px; text-align: left; font-size: 12px; font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        td { border: 1px solid #bbb; padding: 10px; font-size: 12px; vertical-align: top; }");
        html.AppendLine("        tbody tr:nth-child(even) { background: #f9f9f9; }");
        html.AppendLine("        .text-center { text-align: center; }");
        html.AppendLine("        .badge { display: inline-block; padding: 3px 10px; border-radius: 10px; font-size: 11px; font-weight: bold; color: white; }");
        html.AppendLine("        .badge-draft { background-color: #9E9E9E; }");
        html.AppendLine("        .badge-tovalidate { background-color: #607D8B; }");
        html.AppendLine("        .badge-transmitted { background-color: #2196F3; }");
        html.AppendLine("        .badge-inpurchase { background-color: #FFC107; color: #333; }");
        html.AppendLine("        .badge-validated { background-color: #4CAF50; }");
        html.AppendLine("        .badge-cancelled { background-color: #F44336; }");
        html.AppendLine("        .details-table { margin: 5px 0; width: 100%; border-collapse: collapse; }");
        html.AppendLine("        .details-table th { background: #f0f0f0; border: 1px solid #ddd; padding: 5px; font-size: 10px; }");
        html.AppendLine("        .details-table td { border: 1px solid #ddd; padding: 5px; font-size: 10px; }");
        html.AppendLine("        .recap { margin: 30px 0; padding: 15px; background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; }");
        html.AppendLine("        .recap-grid { display: flex; gap: 30px; flex-wrap: wrap; }");
        html.AppendLine("        .recap-item { flex: 1; min-width: 150px; }");
        html.AppendLine("        .recap-label { font-size: 11px; color: #666; }");
        html.AppendLine("        .recap-value { font-size: 18px; font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // En-tête
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"header-left\">");
        html.AppendLine("                <h1>GesAchats v2.0</h1>");
        html.AppendLine("                <p>Module Magasin / Stocks</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"header-right\">");
        html.AppendLine("                <h2>LISTE DES BESOINS</h2>");
        html.AppendLine($"                <p><span class=\"generation-date\">Généré le: {DateTime.Now:dd/MM/yyyy HH:mm}</span></p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Section titre
        html.AppendLine("        <div class=\"section-title\">DEMANDES DE RÉAPPROVISIONNEMENT</div>");

        // Tableau principal
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>N° Demande</th>");
        html.AppendLine("                    <th>Date</th>");
        html.AppendLine("                    <th>Demandeur</th>");
        html.AppendLine("                    <th class=\"text-center\">Nb Articles</th>");
        html.AppendLine("                    <th>Statut</th>");
        html.AppendLine("                    <th>Articles demandés</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");

        int totalArticles = 0;
        foreach (var need in needs)
        {
            int nbArticles = need.Details?.Count ?? 0;
            totalArticles += nbArticles;

            string statusClass = need.Status switch
            {
                NeedStatus.Draft => "badge-draft",
                NeedStatus.ToValidate => "badge-tovalidate",
                NeedStatus.TransmittedToPurchasing => "badge-transmitted",
                NeedStatus.InPurchase => "badge-inpurchase",
                NeedStatus.Validated => "badge-validated",
                NeedStatus.Cancelled => "badge-cancelled",
                NeedStatus.Rejected => "badge-cancelled",
                _ => "badge-draft"
            };

            string statusText = need.Status switch
            {
                NeedStatus.Draft => "En attente",
                NeedStatus.ToValidate => "À Valider",
                NeedStatus.TransmittedToPurchasing => "Transmis",
                NeedStatus.InPurchase => "En cours",
                NeedStatus.Validated => "Complété",
                NeedStatus.Cancelled => "Annulé",
                NeedStatus.Rejected => "Annulé",
                _ => need.Status.ToString()
            };

            html.AppendLine("                <tr>");
            html.AppendLine($"                    <td><strong>{need.NumeroBesoin}</strong></td>");
            html.AppendLine($"                    <td>{need.RequestedAt:dd/MM/yyyy}</td>");
            html.AppendLine($"                    <td>{need.RequestedBy?.FullName ?? "N/A"}</td>");
            html.AppendLine($"                    <td class=\"text-center\">{nbArticles}</td>");
            html.AppendLine($"                    <td><span class=\"badge {statusClass}\">{statusText}</span></td>");
            html.AppendLine("                    <td>");

            if (need.Details != null && need.Details.Any())
            {
                html.AppendLine("                        <table class=\"details-table\">");
                html.AppendLine("                            <thead>");
                html.AppendLine("                                <tr>");
                html.AppendLine("                                    <th>Désignation</th>");
                html.AppendLine("                                    <th class=\"text-center\">Qté</th>");
                html.AppendLine("                                    <th class=\"text-center\">Unité</th>");
                html.AppendLine("                                    <th>Observation</th>");
                html.AppendLine("                                </tr>");
                html.AppendLine("                            </thead>");
                html.AppendLine("                            <tbody>");

                foreach (var detail in need.Details)
                {
                    html.AppendLine("                                <tr>");
                    html.AppendLine($"                                    <td>{detail.Product?.Designation ?? "N/A"}</td>");
                    html.AppendLine($"                                    <td class=\"text-center\">{detail.Quantity:N2}</td>");
                    html.AppendLine($"                                    <td class=\"text-center\">{detail.Product?.Unit ?? "-"}</td>");
                    html.AppendLine($"                                    <td>{(!string.IsNullOrEmpty(need.Notes) ? need.Notes : "")}</td>");
                    html.AppendLine("                                </tr>");
                }

                html.AppendLine("                            </tbody>");
                html.AppendLine("                        </table>");
            }
            else
            {
                html.AppendLine("                        <span style=\"color: #999; font-size: 11px;\">Aucun détail</span>");
            }

            html.AppendLine("                    </td>");
            html.AppendLine("                </tr>");
        }

        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");

        // Récapitulatif
        int totalNeeds = needs.Count;
        int transmitCount = needs.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);
        int inProgressCount = needs.Count(n => n.Status == NeedStatus.InPurchase);
        int completedCount = needs.Count(n => n.Status == NeedStatus.Validated);
        int cancelledCount = needs.Count(n => n.Status == NeedStatus.Cancelled || n.Status == NeedStatus.Rejected);

        html.AppendLine("        <div class=\"recap\">");
        html.AppendLine("            <div class=\"recap-grid\">");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">Total demandes</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{totalNeeds}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">Total articles</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{totalArticles}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">Transmises</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{transmitCount}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">En cours</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{inProgressCount}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">Complétées</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{completedCount}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("                <div class=\"recap-item\">");
        html.AppendLine("                    <div class=\"recap-label\">Annulées</div>");
        html.AppendLine($"                    <div class=\"recap-value\">{cancelledCount}</div>");
        html.AppendLine("                </div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Document généré par GesAchats v2.0 - Système de Gestion des Achats</p>");
        html.AppendLine("            <p>Confidentiel - Usage interne uniquement</p>");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateQuotationRequestPdfAsync(Quotation quotation)
    {
        try
        {
            string htmlContent = GenerateQuotationHtmlContent(quotation);

            string fileName = $"DEVIS_{quotation.ReferenceNumber.Replace("-", "_")}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            string htmlPath = Path.Combine(_outputPath, $"DEVIS_{quotation.ReferenceNumber}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erreur conversion PDF wkhtmltopdf (code {process.ExitCode}). Assurez-vous que wkhtmltopdf est installé.");
                    }
                }
            }

            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF Devis: {ex.Message}", ex);
        }
    }

    private string GenerateQuotationHtmlContent(Quotation quotation)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine($"    <title>Demande de Prix - {quotation.ReferenceNumber}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; color: #333; }");
        html.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
        html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #1a3a52; padding-bottom: 20px; }");
        html.AppendLine("        .header-left h1 { color: #0066cc; font-size: 28px; margin-bottom: 5px; }");
        html.AppendLine("        .header-left p { color: #666; font-size: 14px; }");
        html.AppendLine("        .header-right { text-align: right; }");
        html.AppendLine("        .header-right h2 { color: #ccc; font-size: 42px; font-weight: bold; margin-bottom: 10px; }");
        html.AppendLine("        .header-right p { font-size: 14px; margin: 5px 0; }");
        html.AppendLine("        .generation-date { font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        .section-title { background-color: #1a3a52; color: white; padding: 10px 15px; margin: 20px 0 10px 0; font-size: 13px; font-weight: bold; }");
        html.AppendLine("        .supplier-box { border: 2px solid #1a3a52; padding: 15px; border-radius: 4px; margin-bottom: 20px; }");
        html.AppendLine("        .supplier-box h3 { color: #1a3a52; font-size: 13px; font-weight: bold; margin-bottom: 8px; border-bottom: 1px solid #1a3a52; padding-bottom: 5px; }");
        html.AppendLine("        .supplier-box p { font-size: 12px; line-height: 1.6; color: #555; }");
        html.AppendLine("        .intro-text { font-size: 13px; line-height: 1.6; color: #555; margin-bottom: 20px; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th { background: #e6f0fa; border: 1px solid #bbb; padding: 10px; text-align: left; font-size: 12px; font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        td { border: 1px solid #bbb; padding: 10px; font-size: 12px; }");
        html.AppendLine("        tbody tr:nth-child(even) { background: #f9f9f9; }");
        html.AppendLine("        .text-center { text-align: center; }");
        html.AppendLine("        .info-grid { display: flex; gap: 20px; margin-bottom: 20px; flex-wrap: wrap; }");
        html.AppendLine("        .info-box { flex: 1; min-width: 200px; border: 1px solid #ddd; padding: 15px; border-radius: 4px; background: #f9f9f9; }");
        html.AppendLine("        .info-label { font-size: 10px; color: #999; text-transform: uppercase; }");
        html.AppendLine("        .info-value { font-size: 14px; font-weight: bold; color: #333; margin-bottom: 8px; }");
        html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 40px; border-top: 1px solid #ddd; padding-top: 15px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // En-tête
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"header-left\">");
        html.AppendLine("                <h1>GesAchats v2.0</h1>");
        html.AppendLine("                <p>Module Responsable d'Achat</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"header-right\">");
        html.AppendLine("                <h2>DEMANDE DE PRIX</h2>");
        html.AppendLine($"                <p><strong>Réf : {quotation.ReferenceNumber}</strong></p>");
        html.AppendLine($"                <p>Date : {quotation.Date:dd/MM/yyyy}</p>");
        html.AppendLine($"                <p><span class=\"generation-date\">Généré le: {DateTime.Now:dd/MM/yyyy HH:mm}</span></p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Fournisseur
        html.AppendLine("        <div class=\"supplier-box\">");
        html.AppendLine("            <h3>FOURNISSEUR</h3>");
        html.AppendLine($"            <p><strong>{quotation.Supplier?.CompanyName ?? "N/A"}</strong></p>");
        if (!string.IsNullOrEmpty(quotation.Supplier?.Address))
            html.AppendLine($"            <p>{quotation.Supplier.Address}</p>");
        if (!string.IsNullOrEmpty(quotation.Supplier?.City))
            html.AppendLine($"            <p>{quotation.Supplier.City}</p>");
        if (!string.IsNullOrEmpty(quotation.Supplier?.Phone))
            html.AppendLine($"            <p>Tél : {quotation.Supplier.Phone}</p>");
        if (!string.IsNullOrEmpty(quotation.Supplier?.Email))
            html.AppendLine($"            <p>Email : {quotation.Supplier.Email}</p>");
        html.AppendLine("        </div>");

        // Texte d'introduction
        html.AppendLine("        <div class=\"intro-text\">");
        html.AppendLine("            Madame, Monsieur, nous vous prions de bien vouloir nous faire parvenir votre meilleure offre de prix pour les articles suivants :");
        html.AppendLine("        </div>");

        // Tableau des articles
        html.AppendLine("        <div class=\"section-title\">ARTICLES DEMANDÉS</div>");
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>#</th>");
        html.AppendLine("                    <th>Désignation</th>");
        html.AppendLine("                    <th class=\"text-center\">Quantité</th>");
        html.AppendLine("                    <th class=\"text-center\">Unité</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");

        int i = 1;
        if (quotation.Details != null && quotation.Details.Any())
        {
            foreach (var item in quotation.Details)
            {
                html.AppendLine("                <tr>");
                html.AppendLine($"                    <td>{i++}</td>");
                html.AppendLine($"                    <td>{item.Product?.Designation ?? "N/A"}</td>");
                html.AppendLine($"                    <td class=\"text-center\">{item.Quantity:N2}</td>");
                html.AppendLine($"                    <td class=\"text-center\">{item.Product?.Unit ?? "-"}</td>");
                html.AppendLine("                </tr>");
            }
        }
        else
        {
            html.AppendLine("                <tr>");
            html.AppendLine("                    <td colspan=\"4\" style=\"text-align: center; color: #999;\">Aucun article dans ce devis</td>");
            html.AppendLine("                </tr>");
        }

        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");

        // Informations complémentaires
        html.AppendLine("        <div class=\"section-title\">INFORMATIONS COMPLÉMENTAIRES</div>");
        html.AppendLine("        <div class=\"info-grid\">");
        html.AppendLine("            <div class=\"info-box\">");
        html.AppendLine("                <div class=\"info-label\">Statut</div>");
        html.AppendLine($"                <div class=\"info-value\">{quotation.Status}</div>");
        html.AppendLine("                <div class=\"info-label\">Besoin lié</div>");
        html.AppendLine($"                <div class=\"info-value\">{quotation.Need?.NumeroBesoin ?? "N/A"}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"info-box\">");
        html.AppendLine("                <div class=\"info-label\">Montant Total HT</div>");
        html.AppendLine($"                <div class=\"info-value\">{quotation.TotalAmountHT:N2} MAD</div>");
        html.AppendLine("                <div class=\"info-label\">Montant Total TTC</div>");
        html.AppendLine($"                <div class=\"info-value\">{quotation.TotalAmountTTC:N2} MAD</div>");
        html.AppendLine("            </div>");
        if (!string.IsNullOrEmpty(quotation.Observations))
        {
            html.AppendLine("            <div class=\"info-box\" style=\"flex-basis: 100%;\">");
            html.AppendLine("                <div class=\"info-label\">Observations</div>");
            html.AppendLine($"                <div class=\"info-value\">{quotation.Observations}</div>");
            html.AppendLine("            </div>");
        }
        html.AppendLine("        </div>");

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Document généré par GesAchats v2.0 - Système de Gestion des Achats</p>");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateNeedPdfAsync(Need need)
    {
        try
        {
            string htmlContent = GenerateNeedHtmlContent(need);

            string fileName = $"BESOIN_{need.NumeroBesoin.Replace("-", "_")}.pdf";
            string pdfPath = Path.Combine(_outputPath, fileName);

            string htmlPath = Path.Combine(_outputPath, $"BESOIN_{need.NumeroBesoin}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            var processInfo = new ProcessStartInfo
            {
                FileName = "wkhtmltopdf",
                Arguments = $"--enable-local-file-access \"{htmlPath}\" \"{pdfPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erreur conversion PDF wkhtmltopdf (code {process.ExitCode}). Assurez-vous que wkhtmltopdf est installé.");
                    }
                }
            }

            try { File.Delete(htmlPath); } catch { }

            return pdfPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur génération PDF Besoin: {ex.Message}", ex);
        }
    }

    private string GenerateNeedHtmlContent(Need need)
    {
        StringBuilder html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"fr\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine($"    <title>Fiche Besoin - {need.NumeroBesoin}</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; color: #333; }");
        html.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
        html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #1a3a52; padding-bottom: 20px; }");
        html.AppendLine("        .header-left h1 { color: #0066cc; font-size: 28px; margin-bottom: 5px; }");
        html.AppendLine("        .header-left p { color: #666; font-size: 14px; }");
        html.AppendLine("        .header-right { text-align: right; }");
        html.AppendLine("        .header-right h2 { color: #ccc; font-size: 36px; font-weight: bold; margin-bottom: 10px; }");
        html.AppendLine("        .header-right p { font-size: 14px; margin: 5px 0; }");
        html.AppendLine("        .generation-date { font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        .section-title { background-color: #1a3a52; color: white; padding: 10px 15px; margin: 20px 0 10px 0; font-size: 13px; font-weight: bold; }");
        html.AppendLine("        .info-grid { display: flex; gap: 20px; margin-bottom: 20px; flex-wrap: wrap; }");
        html.AppendLine("        .info-box { flex: 1; min-width: 200px; border: 2px solid #1a3a52; padding: 15px; border-radius: 4px; }");
        html.AppendLine("        .info-box h3 { color: #1a3a52; font-size: 13px; font-weight: bold; margin-bottom: 8px; border-bottom: 1px solid #1a3a52; padding-bottom: 5px; }");
        html.AppendLine("        .info-box p { font-size: 12px; line-height: 1.6; color: #555; }");
        html.AppendLine("        .info-label { font-size: 10px; color: #999; text-transform: uppercase; }");
        html.AppendLine("        .info-value { font-size: 14px; font-weight: bold; color: #333; margin-bottom: 8px; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th { background: #e6f0fa; border: 1px solid #bbb; padding: 10px; text-align: left; font-size: 12px; font-weight: bold; color: #1a3a52; }");
        html.AppendLine("        td { border: 1px solid #bbb; padding: 10px; font-size: 12px; }");
        html.AppendLine("        tbody tr:nth-child(even) { background: #f9f9f9; }");
        html.AppendLine("        .text-center { text-align: center; }");
        html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 40px; border-top: 1px solid #ddd; padding-top: 15px; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // En-tête
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"header-left\">");
        html.AppendLine("                <h1>GesAchats v2.0</h1>");
        html.AppendLine("                <p>Module Magasin / Stocks</p>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"header-right\">");
        html.AppendLine("                <h2>FICHE BESOIN</h2>");
        html.AppendLine($"                <p><span class=\"generation-date\">Généré le: {DateTime.Now:dd/MM/yyyy HH:mm}</span></p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Section titre
        html.AppendLine("        <div class=\"section-title\">DEMANDE DE RÉAPPROVISIONNEMENT</div>");

        html.AppendLine("        <div class=\"info-grid\">");
        html.AppendLine("            <div class=\"info-box\">");
        html.AppendLine("                <h3>INFORMATIONS GÉNÉRALES</h3>");
        html.AppendLine("                <div class=\"info-label\">N° Besoin</div>");
        html.AppendLine($"                <div class=\"info-value\">{need.NumeroBesoin}</div>");
        html.AppendLine("                <div class=\"info-label\">Date création</div>");
        html.AppendLine($"                <div class=\"info-value\">{need.RequestedAt:dd/MM/yyyy HH:mm}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"info-box\">");
        html.AppendLine("                <h3>DEMANDEUR</h3>");
        html.AppendLine("                <div class=\"info-label\">Créé par</div>");
        html.AppendLine($"                <div class=\"info-value\">{need.RequestedBy?.FullName ?? "N/A"}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Articles
        html.AppendLine("        <div class=\"section-title\">ARTICLES DEMANDÉS</div>");
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>#</th>");
        html.AppendLine("                    <th>Désignation</th>");
        html.AppendLine("                    <th class=\"text-center\">Quantité demandée</th>");
        html.AppendLine("                    <th class=\"text-center\">Unité</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");

        int i = 1;
        if (need.Details != null && need.Details.Any())
        {
            foreach (var detail in need.Details)
            {
                html.AppendLine("                <tr>");
                html.AppendLine($"                    <td>{i++}</td>");
                html.AppendLine($"                    <td>{detail.Product?.Designation ?? "N/A"}</td>");
                html.AppendLine($"                    <td class=\"text-center\">{detail.Quantity:N2}</td>");
                html.AppendLine($"                    <td class=\"text-center\">{detail.Product?.Unit ?? "-"}</td>");
                html.AppendLine("                </tr>");
            }
        }
        else
        {
            html.AppendLine("                <tr>");
            html.AppendLine("                    <td colspan=\"4\" style=\"text-align: center; color: #999;\">Aucun article dans cette demande</td>");
            html.AppendLine("                </tr>");
        }

        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");

        // Description / Notes
        if (!string.IsNullOrEmpty(need.Description) || !string.IsNullOrEmpty(need.Notes))
        {
            html.AppendLine("        <div class=\"section-title\">DESCRIPTION / NOTES</div>");
            html.AppendLine("        <div style=\"background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; padding: 15px; margin: 10px 0; font-size: 12px;\">");
            html.AppendLine($"            <p>{(string.IsNullOrEmpty(need.Description) ? need.Notes : need.Description)}</p>");
            html.AppendLine("        </div>");
        }

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>Document généré par GesAchats v2.0 - Système de Gestion des Achats</p>");
        html.AppendLine("            <p>Confidentiel - Usage interne uniquement</p>");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }
}
