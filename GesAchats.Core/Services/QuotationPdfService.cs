using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GesAchats.Core.Services
{
    public interface IQuotationPdfService
    {
        Task<byte[]> GeneratePdfAsync(string htmlContent);
    }

    public class QuotationPdfService : IQuotationPdfService
    {
        public async Task<byte[]> GeneratePdfAsync(string htmlContent)
        {
            string tempHtmlPath = Path.Combine(Path.GetTempPath(), $"quotation_{Guid.NewGuid()}.html");
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"quotation_{Guid.NewGuid()}.pdf");
            try
            {
                // 1. Écrire le HTML dans un fichier temporaire
                await File.WriteAllTextAsync(tempHtmlPath, htmlContent);
                
                // 2. Appeler wkhtmltopdf
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wkhtmltopdf",
                    Arguments = $"\"{tempHtmlPath}\" \"{tempPdfPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        throw new Exception("Impossible de démarrer wkhtmltopdf.");

                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"wkhtmltopdf error: {error}");
                    }
                }
                
                // 3. Lire le PDF généré
                byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdfPath);
                return pdfBytes;
            }
            finally
            {
                // 4. Nettoyer les fichiers temporaires
                if (File.Exists(tempHtmlPath)) File.Delete(tempHtmlPath);
                if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
            }
        }
    }
}
