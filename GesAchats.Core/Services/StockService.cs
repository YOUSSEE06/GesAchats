using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public interface IStockService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetAllProductsWithMagasinAsync();
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task<IEnumerable<Product>> GetRuptureProductsAsync();
    Task<bool> UpdateStockAsync(int productId, decimal quantityChange);
    Task<bool> RecordStockExitAsync(StockExit stockExit);
    Task<IEnumerable<StockExit>> GetAllStockExitsAsync();
}

public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _unitOfWork.Products.GetAllAsync();
    }

    public async Task<IEnumerable<Product>> GetAllProductsWithMagasinAsync()
    {
        return await _unitOfWork.Products.GetAllIncludingAsync(p => p.Magasin);
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _unitOfWork.Products.FindAsync(p => p.CurrentStock <= p.MinimumStock && p.CurrentStock > 0 && p.IsActive);
    }

    public async Task<IEnumerable<Product>> GetRuptureProductsAsync()
    {
        return await _unitOfWork.Products.FindAsync(p => p.CurrentStock <= 0 && p.IsActive);
    }

    public async Task<IEnumerable<Product>> GetNewProductsAsync()
    {
        return await _unitOfWork.Products.FindAsync(p => p.IsNew && p.IsActive);
    }

    public async Task<bool> UpdateStockAsync(int productId, decimal quantityChange)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null) return false;

        product.CurrentStock += quantityChange;
        product.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<bool> RecordStockExitAsync(StockExit stockExit)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(stockExit.ProductId);
        if (product == null || product.CurrentStock < stockExit.Quantity) return false;

        product.CurrentStock -= stockExit.Quantity;
        product.UpdatedAt = DateTime.UtcNow;
        stockExit.StockAfterExit = product.CurrentStock;

        await _unitOfWork.StockExits.AddAsync(stockExit);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<IEnumerable<StockExit>> GetAllStockExitsAsync()
    {
        return await _unitOfWork.StockExits.GetAllWithDetailsAsync();
    }
}
