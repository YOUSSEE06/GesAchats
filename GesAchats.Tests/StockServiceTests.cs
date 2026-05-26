using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using Moq;
using System.Linq.Expressions;

namespace GesAchats.Tests;

public class StockServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly StockService _stockService;

    public StockServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _uowMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _stockService = new StockService(_uowMock.Object);
    }

    [Fact]
    public async Task UpdateStockAsync_ProductExists_UpdatesQuantity()
    {
        // Arrange
        var productId = 1;
        var product = new Product { Id = productId, CurrentStock = 10 };
        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act
        var result = await _stockService.UpdateStockAsync(productId, 5);

        // Assert
        Assert.True(result);
        Assert.Equal(15, product.CurrentStock);
        _uowMock.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsFilteredProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, CurrentStock = 2, MinimumStock = 5, IsActive = true }
        };
        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                        .ReturnsAsync(products);

        // Act
        var result = await _stockService.GetLowStockProductsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result.First().Id);
    }
}
