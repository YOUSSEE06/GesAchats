namespace GesAchats.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SavePaymentProofAsync(int supplierId, int invoiceId, string sourcePath);

    /// <summary>
    /// Les fichiers générés par le logiciel (reçus PDF) restent en local, jamais envoyés sur Supabase.
    /// </summary>
    Task<string> SavePaymentReceiptAsync(int supplierId, int invoiceId, string sourcePath);

    /// <summary>
    /// Fichier IMPORTÉ (pièce jointe de facture, etc.) : envoyé sur Supabase Storage
    /// quand il est configuré (référence distante « sb://bucket/... »), sinon repli local.
    /// </summary>
    Task<string> UploadImportedFileAsync(string bucket, string objectPrefix, string sourcePath);
    bool FileExists(string relativePath);
    string GetFullPath(string relativePath);
    Task<string> GetFullPathAsync(string relativePath);
}
