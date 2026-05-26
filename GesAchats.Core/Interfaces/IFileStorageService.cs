namespace GesAchats.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SavePaymentProofAsync(int supplierId, int invoiceId, string sourcePath);
    Task<string> SavePaymentReceiptAsync(int supplierId, int invoiceId, string sourcePath);
    bool FileExists(string relativePath);
    string GetFullPath(string relativePath);
}
