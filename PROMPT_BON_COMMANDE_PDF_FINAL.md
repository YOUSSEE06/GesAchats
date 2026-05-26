# 📘 PROMPT - GÉNÉRER PDF BON DE COMMANDE (LaTeX/HTML + C#)

## 📌 CONTEXTE

**Module** : Espace Responsable d'Achat  
**Document** : Bon de Commande (BC)  
**Format** : PDF généré depuis HTML/CSS  
**Framework** : C# WPF  

---

## 🎯 OBJECTIF

Générer un **PDF professionnel de Bon de Commande** avec :
- ✅ Numéro de BC et date
- ✅ Informations du fournisseur (nom, adresse, contact)
- ✅ Lieu de livraison
- ✅ Articles commandés (désignation, quantité, prix HT/TTC)
- ✅ Résumé financier (Total HT, TVA, Total TTC)
- ✅ Conditions commerciales (paiement, délai, lieu)
- ✅ Observations/instructions spéciales
- ✅ Signatures (Responsable Achats + Directeur)

---

## 📋 STRUCTURE DU BON DE COMMANDE

### **Informations Générales**
```
N°BC: BC-2026-EFBF
Date: 03/05/2026
Besoin d'origine: BES-001
Statut: Émis
```

### **Informations Fournisseur**
```
Raison Sociale: Cimenterie du Nord
Adresse: Tanger, Maroc
Contact: sales@cimentnord.com
Conditions Paiement: Net 30 jours
Délai Livraison: 5 jours ouvrables
```

### **Lieu de Livraison**
```
GesAchats - Siège Social
123 Rue de l'Industrie
Casablanca, Maroc
```

### **Tableau Articles**
```
N° | Désignation | Quantité | Unité | PU HT | PU TTC | Total TTC
1  | Graviers 40mm | 20 | m³ | 14,50 MAD | 17,40 MAD | 348,00 MAD
```

### **Résumé Financier**
```
Total HT: 290,00 MAD
TVA (20%): 58,00 MAD
TOTAL TTC: 348,00 MAD
```

### **Conditions Commerciales**
```
Mode de Paiement: Virement bancaire
Conditions Paiement: Net 30 jours
Délai de Livraison: 5 jours ouvrables
Lieu de Livraison: Casablanca, Maroc
```

---

## 🏗️ TEMPLATE HTML/CSS (RECOMMANDÉ)

### **Avantages**
✅ Facile à générer en C#  
✅ Compatible wkhtmltopdf  
✅ Responsive et moderne  
✅ Pas de dépendances externes (contrairement à LaTeX)  

### **Structure HTML complète**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bon de Commande - BC-2026-EFBF</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Arial', sans-serif;
            background-color: #f5f5f5;
            padding: 20px;
            color: #333;
        }

        .container {
            max-width: 900px;
            margin: 0 auto;
            background-color: white;
            padding: 40px;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }

        /* En-tête */
        .header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 30px;
            border-bottom: 3px solid #1a3a52;
            padding-bottom: 20px;
        }

        .header-left h1 {
            color: #0066cc;
            font-size: 28px;
            margin-bottom: 5px;
        }

        .header-left p {
            color: #666;
            font-size: 14px;
        }

        .header-right {
            text-align: right;
        }

        .header-right h2 {
            color: #ccc;
            font-size: 48px;
            font-weight: bold;
            margin-bottom: 10px;
        }

        .header-right p {
            font-size: 14px;
            margin: 5px 0;
        }

        .bc-number {
            font-weight: bold;
            color: #1a3a52;
        }

        /* Sections */
        .section-title {
            background-color: #1a3a52;
            color: white;
            padding: 10px 15px;
            margin: 20px 0 10px 0;
            font-size: 13px;
            font-weight: bold;
        }

        /* Boîtes info */
        .boxes {
            display: flex;
            gap: 20px;
            margin-bottom: 30px;
            flex-wrap: wrap;
        }

        .box {
            flex: 1;
            min-width: 250px;
            border: 2px solid #1a3a52;
            padding: 15px;
            border-radius: 4px;
        }

        .box h3 {
            color: #1a3a52;
            font-size: 13px;
            font-weight: bold;
            margin-bottom: 8px;
        }

        .box p {
            font-size: 12px;
            line-height: 1.6;
            color: #555;
        }

        /* Tableau articles */
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }

        table thead {
            background-color: #e6f0fa;
        }

        table th {
            border: 1px solid #bbb;
            padding: 10px;
            text-align: left;
            font-size: 12px;
            font-weight: bold;
            color: #1a3a52;
        }

        table td {
            border: 1px solid #bbb;
            padding: 10px;
            font-size: 12px;
        }

        table tbody tr:nth-child(even) {
            background-color: #f9f9f9;
        }

        .text-right {
            text-align: right;
        }

        .text-center {
            text-align: center;
        }

        /* Résumé financier */
        .resume-financier {
            text-align: right;
            margin: 30px 0;
        }

        .resume-table {
            display: inline-block;
            border: 1px solid #bbb;
            border-collapse: collapse;
        }

        .resume-row {
            display: flex;
            border-bottom: 1px solid #bbb;
        }

        .resume-label {
            text-align: left;
            font-weight: bold;
            background-color: #f0f0f0;
            padding: 10px 20px;
            min-width: 150px;
            border-right: 1px solid #bbb;
        }

        .resume-value {
            text-align: right;
            padding: 10px 20px;
            min-width: 100px;
            font-size: 13px;
        }

        .total-ttc {
            background-color: #1a3a52;
            color: white;
            font-weight: bold;
        }

        .total-ttc .resume-value {
            color: #0066cc;
            font-size: 16px;
        }

        /* Conditions */
        .conditions {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin: 20px 0;
            font-size: 12px;
        }

        .condition-item {
            background-color: #f9f9f9;
            padding: 10px;
            border-left: 3px solid #0066cc;
        }

        .condition-label {
            font-weight: bold;
            color: #1a3a52;
            margin-bottom: 3px;
        }

        .condition-value {
            color: #555;
        }

        /* Observations */
        .observations {
            background-color: #f9f9f9;
            border: 1px solid #ddd;
            border-radius: 4px;
            padding: 15px;
            margin: 20px 0;
            font-size: 12px;
            line-height: 1.6;
        }

        .observations h4 {
            color: #1a3a52;
            margin-bottom: 8px;
            font-size: 12px;
        }

        /* Signatures */
        .signatures {
            display: flex;
            justify-content: space-between;
            margin: 40px 0 20px 0;
            font-size: 11px;
        }

        .signature-block {
            flex: 1;
            margin: 0 10px;
            text-align: center;
        }

        .signature-block p {
            margin: 5px 0;
        }

        .signature-line {
            margin-top: 30px;
            border-top: 1px solid #333;
            padding-top: 10px;
        }

        /* Pied de page */
        .footer {
            text-align: center;
            font-size: 10px;
            color: #999;
            margin-top: 30px;
            border-top: 1px solid #ddd;
            padding-top: 15px;
        }

        /* Impression */
        @media print {
            body {
                background-color: white;
                padding: 0;
            }
            .container {
                box-shadow: none;
                padding: 10px;
            }
        }
    </style>
</head>
<body>
    <div class="container">
        <!-- En-tête -->
        <div class="header">
            <div class="header-left">
                <h1>GesAchats v2.0</h1>
                <p>Module Responsable d'Achat</p>
            </div>
            <div class="header-right">
                <h2>BON DE<br>COMMANDE</h2>
                <p><span class="bc-number">N°: BC-2026-EFBF</span></p>
                <p>Date: 03/05/2026</p>
            </div>
        </div>

        <!-- Fournisseur et Livraison -->
        <div class="boxes">
            <div class="box">
                <h3>FOURNISSEUR</h3>
                <p>
                    <strong>Cimenterie du Nord</strong><br>
                    Tanger<br>
                    sales@cimentnord.com
                </p>
            </div>
            <div class="box">
                <h3>EXPÉDIÉ À</h3>
                <p>
                    <strong>GesAchats - Siège Social</strong><br>
                    123 Rue de l'Industrie<br>
                    Casablanca, Maroc
                </p>
            </div>
            <div class="box">
                <h3>INFORMATIONS</h3>
                <p>
                    <strong>Référence Besoin:</strong> BES-001<br>
                    <strong>Conditions Paiement:</strong> Net 30 jours<br>
                    <strong>Délai Livraison:</strong> 5 jours ouvrables
                </p>
            </div>
        </div>

        <!-- Articles -->
        <div class="section-title">ARTICLES COMMANDÉS</div>
        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Désignation</th>
                    <th class="text-center">Quantité</th>
                    <th>Unité</th>
                    <th class="text-right">PU HT</th>
                    <th class="text-right">PU TTC</th>
                    <th class="text-right">Total TTC</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>1</td>
                    <td>Graviers 40mm</td>
                    <td class="text-center">20</td>
                    <td>m³</td>
                    <td class="text-right">14,50 MAD</td>
                    <td class="text-right">17,40 MAD</td>
                    <td class="text-right"><strong>348,00 MAD</strong></td>
                </tr>
            </tbody>
        </table>

        <!-- Résumé financier -->
        <div class="resume-financier">
            <div class="resume-table">
                <div class="resume-row">
                    <div class="resume-label">Total HT</div>
                    <div class="resume-value">290,00 MAD</div>
                </div>
                <div class="resume-row">
                    <div class="resume-label">TVA (20%)</div>
                    <div class="resume-value">58,00 MAD</div>
                </div>
                <div class="resume-row total-ttc">
                    <div class="resume-label">TOTAL TTC</div>
                    <div class="resume-value">348,00 MAD</div>
                </div>
            </div>
        </div>

        <!-- Conditions commerciales -->
        <div class="section-title">CONDITIONS COMMERCIALES</div>
        <div class="conditions">
            <div class="condition-item">
                <div class="condition-label">Mode de Paiement</div>
                <div class="condition-value">Virement bancaire</div>
            </div>
            <div class="condition-item">
                <div class="condition-label">Conditions Paiement</div>
                <div class="condition-value">Net 30 jours</div>
            </div>
            <div class="condition-item">
                <div class="condition-label">Délai de Livraison</div>
                <div class="condition-value">5 jours ouvrables</div>
            </div>
            <div class="condition-item">
                <div class="condition-label">Lieu de Livraison</div>
                <div class="condition-value">Casablanca, Maroc</div>
            </div>
        </div>

        <!-- Observations -->
        <div class="section-title">OBSERVATIONS</div>
        <div class="observations">
            <p>Livraison sur site principal. Vérifier la qualité et les quantités à la réception. 
            Tout défaut doit être signalé dans les 48 heures.</p>
        </div>

        <!-- Signatures -->
        <div class="section-title">VALIDATIONS</div>
        <div class="signatures">
            <div class="signature-block">
                <p><strong>Créé par:</strong></p>
                <p style="margin-top: 20px; font-size: 12px;">Responsable Achats</p>
                <div class="signature-line">
                    <p>Date: 03/05/2026</p>
                    <p>Signature: ___________</p>
                </div>
            </div>
            <div class="signature-block">
                <p><strong>Approuvé par:</strong></p>
                <p style="margin-top: 20px; font-size: 12px;">Directeur Administratif</p>
                <div class="signature-line">
                    <p>Date: ___________</p>
                    <p>Signature: ___________</p>
                </div>
            </div>
        </div>

        <!-- Pied de page -->
        <div class="footer">
            <p>Document généré par GesAchats v2.0 - Système de Gestion des Achats et Approvisionnements</p>
            <p>Confidentiel - Usage interne uniquement</p>
        </div>
    </div>
</body>
</html>
```

---

## 🛠️ CLASSE C# - BonCommandePdfService

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace GesAchats.Services
{
    public class BonCommandePdfService
    {
        private readonly string _outputPath;

        public BonCommandePdfService()
        {
            _outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
            Directory.CreateDirectory(_outputPath);
        }

        /// <summary>
        /// Générer un PDF Bon de Commande
        /// </summary>
        public string GenerateBonCommandePdf(BonCommande bc, List<BcDetail> details)
        {
            try
            {
                // 1. Générer le HTML
                string htmlContent = GenerateHtmlContent(bc, details);

                // 2. Convertir en PDF avec wkhtmltopdf
                string pdfPath = ConvertHtmlToPdf(htmlContent, bc.NumeroBc);

                return pdfPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur génération PDF BC: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Générer le contenu HTML
        /// </summary>
        private string GenerateHtmlContent(BonCommande bc, List<BcDetail> details)
        {
            StringBuilder html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"fr\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine($"    <title>Bon de Commande - {bc.NumeroBc}</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            html.AppendLine("        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }");
            html.AppendLine("        .container { max-width: 900px; margin: 0 auto; background: white; padding: 40px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }");
            html.AppendLine("        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #1a3a52; padding-bottom: 20px; }");
            html.AppendLine("        .header h1 { color: #0066cc; font-size: 28px; }");
            html.AppendLine("        .header h2 { color: #ccc; font-size: 48px; margin-bottom: 10px; }");
            html.AppendLine("        .header p { font-size: 14px; margin: 5px 0; }");
            html.AppendLine("        .bc-number { font-weight: bold; color: #1a3a52; }");
            html.AppendLine("        .section-title { background: #1a3a52; color: white; padding: 10px 15px; margin: 20px 0 10px 0; font-weight: bold; }");
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
            html.AppendLine("        .resume-table { display: inline-block; border: 1px solid #bbb; }");
            html.AppendLine("        .resume-row { display: flex; border-bottom: 1px solid #bbb; }");
            html.AppendLine("        .resume-label { text-align: left; font-weight: bold; background: #f0f0f0; padding: 10px 20px; min-width: 150px; border-right: 1px solid #bbb; }");
            html.AppendLine("        .resume-value { text-align: right; padding: 10px 20px; min-width: 100px; font-size: 13px; }");
            html.AppendLine("        .total-ttc { background: #1a3a52; color: white; font-weight: bold; }");
            html.AppendLine("        .total-ttc .resume-value { color: #0066cc; font-size: 16px; }");
            html.AppendLine("        .conditions { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin: 20px 0; font-size: 12px; }");
            html.AppendLine("        .condition-item { background: #f9f9f9; padding: 10px; border-left: 3px solid #0066cc; }");
            html.AppendLine("        .condition-label { font-weight: bold; color: #1a3a52; margin-bottom: 3px; }");
            html.AppendLine("        .observations { background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px; padding: 15px; margin: 20px 0; font-size: 12px; line-height: 1.6; }");
            html.AppendLine("        .signatures { display: flex; justify-content: space-between; margin: 40px 0; font-size: 11px; }");
            html.AppendLine("        .signature-block { flex: 1; margin: 0 10px; text-align: center; }");
            html.AppendLine("        .signature-block p { margin: 5px 0; }");
            html.AppendLine("        .signature-line { margin-top: 30px; border-top: 1px solid #333; padding-top: 10px; }");
            html.AppendLine("        .footer { text-align: center; font-size: 10px; color: #999; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }");
            html.AppendLine("        @media print { body { padding: 0; } .container { box-shadow: none; } }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // En-tête
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine("        <div class=\"header\">");
            html.AppendLine("            <div class=\"header-left\">");
            html.AppendLine("                <h1>GesAchats v2.0</h1>");
            html.AppendLine("                <p>Module Responsable d'Achat</p>");
            html.AppendLine("            </div>");
            html.AppendLine("            <div class=\"header-right\">");
            html.AppendLine("                <h2>BON DE<br>COMMANDE</h2>");
            html.AppendLine($"                <p><span class=\"bc-number\">N°: {bc.NumeroBc}</span></p>");
            html.AppendLine($"                <p>Date: {bc.Date:dd/MM/yyyy}</p>");
            html.AppendLine("            </div>");
            html.AppendLine("        </div>");

            // Boîtes info
            html.AppendLine("        <div class=\"boxes\">");
            html.AppendLine("            <div class=\"box\">");
            html.AppendLine("                <h3>FOURNISSEUR</h3>");
            html.AppendLine($"                <p><strong>{bc.Fournisseur}</strong><br>");
            html.AppendLine($"                {bc.AdresseFournisseur}<br>");
            html.AppendLine($"                {bc.ContactFournisseur}</p>");
            html.AppendLine("            </div>");
            html.AppendLine("            <div class=\"box\">");
            html.AppendLine("                <h3>EXPÉDIÉ À</h3>");
            html.AppendLine($"                <p><strong>GesAchats - Siège Social</strong><br>");
            html.AppendLine($"                123 Rue de l'Industrie<br>");
            html.AppendLine($"                Casablanca, Maroc</p>");
            html.AppendLine("            </div>");
            html.AppendLine("            <div class=\"box\">");
            html.AppendLine("                <h3>INFORMATIONS</h3>");
            html.AppendLine($"                <p><strong>Référence Besoin:</strong> {bc.BesoinId ?? 0}<br>");
            html.AppendLine($"                <strong>Conditions Paiement:</strong> {bc.ConditionsPaiement}<br>");
            html.AppendLine($"                <strong>Délai Livraison:</strong> {bc.DelaiLivraisonDemande} jours</p>");
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

            decimal totalHt = 0;
            decimal totalTtc = 0;

            for (int i = 0; i < details.Count; i++)
            {
                var detail = details[i];
                decimal subtotalTtc = detail.Quantite * detail.PuTtc;
                totalTtc += subtotalTtc;
                totalHt += detail.Quantite * detail.PuHt;

                html.AppendLine("                <tr>");
                html.AppendLine($"                    <td>{i + 1}</td>");
                html.AppendLine($"                    <td>{detail.Designation}</td>");
                html.AppendLine($"                    <td class=\"text-center\">{detail.Quantite}</td>");
                html.AppendLine($"                    <td>{detail.Unite}</td>");
                html.AppendLine($"                    <td class=\"text-right\">{detail.PuHt:F2} {detail.Devise}</td>");
                html.AppendLine($"                    <td class=\"text-right\">{detail.PuTtc:F2} {detail.Devise}</td>");
                html.AppendLine($"                    <td class=\"text-right\"><strong>{subtotalTtc:F2} {detail.Devise}</strong></td>");
                html.AppendLine("                </tr>");
            }

            html.AppendLine("            </tbody>");
            html.AppendLine("        </table>");

            // Résumé financier
            html.AppendLine("        <div class=\"resume-financier\">");
            html.AppendLine("            <div class=\"resume-table\">");
            html.AppendLine("                <div class=\"resume-row\">");
            html.AppendLine($"                    <div class=\"resume-label\">Total HT</div>");
            html.AppendLine($"                    <div class=\"resume-value\">{totalHt:F2} MAD</div>");
            html.AppendLine("                </div>");
            html.AppendLine("                <div class=\"resume-row\">");
            html.AppendLine($"                    <div class=\"resume-label\">TVA (20%)</div>");
            html.AppendLine($"                    <div class=\"resume-value\">{totalTtc - totalHt:F2} MAD</div>");
            html.AppendLine("                </div>");
            html.AppendLine("                <div class=\"resume-row total-ttc\">");
            html.AppendLine($"                    <div class=\"resume-label\">TOTAL TTC</div>");
            html.AppendLine($"                    <div class=\"resume-value\">{totalTtc:F2} MAD</div>");
            html.AppendLine("                </div>");
            html.AppendLine("            </div>");
            html.AppendLine("        </div>");

            // Conditions commerciales
            html.AppendLine("        <div class=\"section-title\">CONDITIONS COMMERCIALES</div>");
            html.AppendLine("        <div class=\"conditions\">");
            html.AppendLine("            <div class=\"condition-item\">");
            html.AppendLine("                <div class=\"condition-label\">Mode de Paiement</div>");
            html.AppendLine($"                <div class=\"condition-value\">{bc.ModePaiement}</div>");
            html.AppendLine("            </div>");
            html.AppendLine("            <div class=\"condition-item\">");
            html.AppendLine("                <div class=\"condition-label\">Conditions Paiement</div>");
            html.AppendLine($"                <div class=\"condition-value\">{bc.ConditionsPaiement}</div>");
            html.AppendLine("            </div>");
            html.AppendLine("            <div class=\"condition-item\">");
            html.AppendLine("                <div class=\"condition-label\">Délai de Livraison</div>");
            html.AppendLine($"                <div class=\"condition-value\">{bc.DelaiLivraisonDemande} jours ouvrables</div>");
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
            html.AppendLine($"                <p style=\"margin-top: 20px; font-size: 12px;\">{bc.CreePar}</p>");
            html.AppendLine("                <p>Responsable Achats</p>");
            html.AppendLine("                <div class=\"signature-line\">");
            html.AppendLine($"                    <p>Date: {bc.Date:dd/MM/yyyy}</p>");
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

        /// <summary>
        /// Convertir HTML en PDF avec wkhtmltopdf
        /// </summary>
        private string ConvertHtmlToPdf(string htmlContent, string numeroBc)
        {
            // Sauvegarder HTML temporaire
            string htmlPath = Path.Combine(_outputPath, $"BC_{numeroBc}_{Guid.NewGuid()}.html");
            File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);

            string pdfPath = Path.Combine(_outputPath, $"BON_COMMANDE_{numeroBc}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

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
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Erreur conversion PDF (code {process.ExitCode})");
                }
            }

            // Nettoyer HTML temporaire
            try { File.Delete(htmlPath); }
            catch { }

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF non généré: {pdfPath}");

            return pdfPath;
        }

        /// <summary>
        /// Ouvrir le PDF généré
        /// </summary>
        public void OpenPdf(string pdfPath)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException($"PDF non trouvé: {pdfPath}");

            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
    }
}
```

---

## 📊 MODELS C#

```csharp
public class BonCommande
{
    public int BcId { get; set; }
    public string NumeroBc { get; set; }
    public DateTime Date { get; set; }
    public int FournisseurId { get; set; }
    public string Fournisseur { get; set; }
    public string AdresseFournisseur { get; set; }
    public string ContactFournisseur { get; set; }
    public int? BesoinId { get; set; }
    public int? DevisId { get; set; }
    public string ModePaiement { get; set; }
    public string ConditionsPaiement { get; set; }
    public int DelaiLivraisonDemande { get; set; }
    public string Observations { get; set; }
    public string CreePar { get; set; }
    public string Statut { get; set; }
    public string FichierPdf { get; set; }
    public DateTime DateMaj { get; set; }
}

public class BcDetail
{
    public int DetailId { get; set; }
    public int BcId { get; set; }
    public int ArticleId { get; set; }
    public string Designation { get; set; }
    public int Quantite { get; set; }
    public string Unite { get; set; }
    public decimal PuHt { get; set; }
    public decimal PuTtc { get; set; }
    public string Devise { get; set; } = "MAD";
}
```

---

## 🔧 UTILISATION DANS LE VIEWMODEL

```csharp
public class CreatePurchaseOrderViewModel : ViewModelBase
{
    private readonly BonCommandePdfService _pdfService;

    public CreatePurchaseOrderViewModel()
    {
        _pdfService = new BonCommandePdfService();
    }

    // Commande pour générer et afficher l'aperçu PDF
    public RelayCommand GeneratePreviewCommand => new RelayCommand(() =>
    {
        try
        {
            var bcDetails = GetBcDetails();
            string pdfPath = _pdfService.GenerateBonCommandePdf(CurrentBonCommande, bcDetails);
            _pdfService.OpenPdf(pdfPath);
            MessageBox.Show("PDF généré avec succès!", "Succès");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur: {ex.Message}");
        }
    });

    // Commande pour émettre et envoyer le BC
    public RelayCommand EmitAndSendCommand => new RelayCommand(() =>
    {
        try
        {
            if (!ValidateBonCommande()) return;

            var bcDetails = GetBcDetails();
            string pdfPath = _pdfService.GenerateBonCommandePdf(CurrentBonCommande, bcDetails);

            CurrentBonCommande.FichierPdf = pdfPath;
            CurrentBonCommande.Statut = "Émis";
            SaveBonCommandeToDatabase();

            MessageBox.Show("BC émis et enregistré avec succès!", "Succès");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur: {ex.Message}");
        }
    });
}
```

---

## ✅ CHECKLIST

- [ ] Créer la classe `BonCommandePdfService.cs`
- [ ] Créer les models `BonCommande` et `BcDetail`
- [ ] Installer wkhtmltopdf sur les machines clients
- [ ] Intégrer dans le ViewModel de création BC
- [ ] Tester la génération PDF
- [ ] Ajouter les boutons dans la Vue (Aperçu PDF, Émettre)
- [ ] Tester l'envoi par email (optionnel)

---

## 📌 NOTES IMPORTANTES

1. **wkhtmltopdf doit être installé**
   - Windows: https://wkhtmltopdf.org/
   - Linux: `sudo apt-get install wkhtmltopdf`
   - Mac: `brew install wkhtmltopdf`

2. **Qualité d'impression**
   - PDF optimisé pour A4 portrait
   - Haute résolution pour impression

3. **Devise**
   - Template utilise MAD (Dirham marocain)
   - Paramétrable via modèle

4. **Personnalisation**
   - Toutes les données sont dynamiques
   - Logo peut être ajouté facilement

---

**STATUS** : ✅ Prêt pour implémentation immédiate

**TEMPS ESTIMÉ** : 8-10 heures

**LIVRABLES** : Service PDF complet + Models + Intégration WPF
