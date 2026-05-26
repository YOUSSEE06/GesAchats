using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileStorageService(string basePath)
    {
        _basePath = basePath;
        
        // S'assurer que les dossiers de base existent
        Directory.CreateDirectory(Path.Combine(_basePath, "Documents", "Preuves"));
        Directory.CreateDirectory(Path.Combine(_basePath, "Documents", "Recus"));
    }

    public async Task<string> SavePaymentProofAsync(int supplierId, int invoiceId, string sourcePath)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Le fichier source est introuvable", sourcePath);

        string extension = Path.GetExtension(sourcePath);
        string dateFolder = DateTime.Now.ToString("yyyyMM");
        string relativeDir = Path.Combine("Documents", "Preuves", dateFolder, supplierId.ToString());
        string fullDir = Path.Combine(_basePath, relativeDir);

        Directory.CreateDirectory(fullDir);

        string fileName = $"RECU_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(fullDir, fileName);

        await Task.Run(() => File.Copy(sourcePath, fullPath, true));

        return Path.Combine(relativeDir, fileName);
    }

    public async Task<string> SavePaymentReceiptAsync(int supplierId, int invoiceId, string sourcePath)
    {
        // Similaire à SavePaymentProof mais dans le dossier Recus
        string extension = Path.GetExtension(sourcePath);
        string dateFolder = DateTime.Now.ToString("yyyyMM");
        string relativeDir = Path.Combine("Documents", "Recus", dateFolder);
        string fullDir = Path.Combine(_basePath, relativeDir);

        Directory.CreateDirectory(fullDir);

        string fileName = $"RECU_GEN_{invoiceId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(fullDir, fileName);

        await Task.Run(() => File.Copy(sourcePath, fullPath, true));

        return Path.Combine(relativeDir, fileName);
    }

    public bool FileExists(string relativePath)
    {
        return File.Exists(Path.Combine(_basePath, relativePath));
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_basePath, relativePath);
    }
}
