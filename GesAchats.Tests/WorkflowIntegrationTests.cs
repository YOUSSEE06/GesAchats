using GesAchats.Core.Entities;
using GesAchats.Data;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GesAchats.Tests;

public class WorkflowIntegrationTests
{
    private GesAchatsDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GesAchatsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GesAchatsDbContext(options);
    }

    [Fact]
    public async Task FullWorkflow_FromQuotationToStockUpdate_Success()
    {
        // 1. Setup Context and UnitOfWork
        using var context = GetInMemoryContext();
        var uow = new UnitOfWork(context);

        // 2. Create Prerequisites (User, Supplier, Product)
        var user = new User { Login = "testuser", Email = "test@test.com", PasswordHash = "hash" };
        var supplier = new Supplier { CompanyName = "Supplier A", Email = "s@a.com", IsActive = true };
        var product = new Product { Designation = "Product 1", CurrentStock = 10, MinimumStock = 5, Unit = "pcs", IsActive = true };
        
        await uow.Users.AddAsync(user);
        await uow.Suppliers.AddAsync(supplier);
        await uow.Products.AddAsync(product);
        await uow.CompleteAsync();

        // 3. Create Quotation
        var quotation = new Quotation 
        { 
            ReferenceNumber = "QUO-001", 
            SupplierId = supplier.Id, 
            Status = "Validated",
            CreatedById = user.Id,
            Details = new List<QuotationDetail>
            {
                new QuotationDetail { ProductId = product.Id, Quantity = 20, UnitPriceHT = 100 }
            }
        };
        await uow.Quotations.AddAsync(quotation);
        await uow.CompleteAsync();

        // 4. Create Purchase Order from Quotation
        var po = new PurchaseOrder
        {
            OrderNumber = "PO-001",
            SupplierId = supplier.Id,
            QuotationId = quotation.Id,
            TotalAmountHT = 2000,
            TotalAmountTTC = 2400,
            Status = "Issued",
            CreatedById = user.Id,
            Details = new List<PurchaseOrderDetail>
            {
                new PurchaseOrderDetail { ProductId = product.Id, Quantity = 20, UnitPriceHT = 100 }
            }
        };
        await uow.PurchaseOrders.AddAsync(po);
        await uow.CompleteAsync();

        // 5. Create Delivery Note and Update Stock
        var dn = new DeliveryNote
        {
            DeliveryNumber = "DN-001",
            PurchaseOrderId = po.Id,
            SupplierId = supplier.Id,
            ReceivedQuantity = 20,
            CompliantQuantity = 18, // 18 compliant
            DefectiveQuantity = 2,
            Status = "FullyReceived",
            ReceivedById = user.Id
        };
        await uow.DeliveryNotes.AddAsync(dn);
        
        // Simulating the stock update logic found in DeliveryNoteViewModel
        var dbProduct = await uow.Products.GetByIdAsync(product.Id);
        dbProduct!.CurrentStock += dn.CompliantQuantity;
        uow.Products.Update(dbProduct);
        
        await uow.CompleteAsync();

        // 6. Assertions
        var finalProduct = await uow.Products.GetByIdAsync(product.Id);
        Assert.Equal(28, finalProduct!.CurrentStock); // 10 initial + 18 compliant
        Assert.Equal("FullyReceived", (await uow.DeliveryNotes.GetByIdAsync(dn.Id))!.Status);
    }

    [Fact]
    public async Task Performance_BulkInsert_MeasuresTime()
    {
        using var context = GetInMemoryContext();
        var uow = new UnitOfWork(context);
        
        int recordCount = 1000;
        var products = new List<Product>();
        for (int i = 0; i < recordCount; i++)
        {
            products.Add(new Product { Designation = $"Product {i}", Unit = "pcs" });
        }

        var sw = Stopwatch.StartNew();
        
        foreach(var p in products)
        {
            await uow.Products.AddAsync(p);
        }
        await uow.CompleteAsync();
        
        sw.Stop();

        // Assertion: Ensure it takes less than 2 seconds for 1000 records in-memory
        // (This is a very basic performance test)
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Bulk insert took too long: {sw.ElapsedMilliseconds}ms");
    }
}
