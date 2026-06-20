using Microsoft.EntityFrameworkCore;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.DTOs;

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
    Task<int> GetTrackedProductsCountAsync();
    Task<int> GetLowStockProductsCountAsync();
    Task<List<ProductPriceAnalysisData>> GetProductPriceAnalysisAsync(DateTime startDate, DateTime endDate, int topCount);
    Task<PagedResult<StockGlobalDto>> GetStockGlobalPagedAsync(int pageNumber, int pageSize, string? searchText, string? selectedStatus, CancellationToken cancellationToken);
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

    public async Task<int> GetTrackedProductsCountAsync()
    {
        var products = await _unitOfWork.Products.FindAsync(p => p.IsActive);
        return products.Count();
    }

    public async Task<int> GetLowStockProductsCountAsync()
    {
        var products = await GetLowStockProductsAsync();
        return products.Count();
    }

    public async Task<List<ProductPriceAnalysisData>> GetProductPriceAnalysisAsync(DateTime startDate, DateTime endDate, int topCount)
    {
        // Get purchase order details
        var allOrders = await _unitOfWork.PurchaseOrders.GetAllIncludingAsync(po => po.Details);
        var filteredOrders = allOrders.Where(po => po.CreatedAt >= startDate && po.CreatedAt <= endDate);
        
        var productQuantities = new Dictionary<int, (decimal quantity, decimal totalPrice)>();
        
        foreach (var order in filteredOrders)
        {
            foreach (var detail in order.Details)
            {
                if (!productQuantities.ContainsKey(detail.ProductId))
                {
                    productQuantities[detail.ProductId] = (0, 0);
                }
                var current = productQuantities[detail.ProductId];
                productQuantities[detail.ProductId] = (
                    current.quantity + detail.Quantity,
                    current.totalPrice + (detail.Quantity * detail.UnitPriceTTC)
                );
            }
        }
        
        // Get product names
        var products = await _unitOfWork.Products.GetAllAsync();
        var productDict = products.ToDictionary(p => p.Id);
        
        var result = productQuantities
            .OrderByDescending(x => x.Value.quantity)
            .Take(topCount)
            .Select(x => 
            {
                var product = productDict.TryGetValue(x.Key, out var p) ? p : null;
                var avgPrice = x.Value.quantity > 0 ? x.Value.totalPrice / x.Value.quantity : 0;
                return new ProductPriceAnalysisData
                {
                    Name = product?.Designation ?? $"Product {x.Key}",
                    Quantity = x.Value.quantity,
                    UnitPrice = avgPrice,
                    PriceChangePercentage = 0,
                    EvolutionText = "Stable"
                };
            })
            .ToList();
            
        return result;
    }

    public async Task<PagedResult<StockGlobalDto>> GetStockGlobalPagedAsync(int pageNumber, int pageSize, string? searchText, string? selectedStatus, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Products.GetQueryable(true);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p => p.Designation.Contains(searchText));
        }

        // Apply status filter directly in EF Core query
        IQueryable<Product> filteredQuery = query;
        if (!string.IsNullOrWhiteSpace(selectedStatus) && selectedStatus != "Tous")
        {
            filteredQuery = selectedStatus switch
            {
                "OK" => filteredQuery.Where(p => p.CurrentStock > p.MinimumStock),
                "Alerte" => filteredQuery.Where(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock),
                "Rupture" => filteredQuery.Where(p => p.CurrentStock == 0),
                _ => filteredQuery
            };
        }

        // Get total count
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        // Get paginated items with projection to DTO
        var finalItems = await filteredQuery
            .OrderBy(p => p.Designation)
            .ThenBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new StockGlobalDto
            {
                Id = p.Id,
                Designation = p.Designation,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                Unit = p.Unit,
                State = p.CurrentStock == 0 ? StockState.OutOfStock :
                        p.CurrentStock <= p.MinimumStock ? StockState.Alert : StockState.Ok
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StockGlobalDto>
        {
            Items = finalItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
