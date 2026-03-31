using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using POS.Models;

namespace POS.Controllers;

public class SalesController : Controller
{
    private readonly AppDbContext _context;
    private const int PageSize = 20;

    public SalesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Sales
    public async Task<IActionResult> Index(int pageNumber = 1, string searchTerm = "", int? customerId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s => s.Customer.Name.Contains(searchTerm) || s.Customer.Phone.Contains(searchTerm));
        }

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.SaleDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(s => s.SaleDate <= endDate);
        }

        // Order by newest first
        query = query.OrderByDescending(s => s.SaleDate);

        // Create paginated list
        var paginatedSales = await PaginatedList<Sale>.CreateAsync(query, pageNumber, PageSize);

        // Pass data to view
        ViewBag.Customers = await GetCustomersSelectList();
        ViewBag.CurrentSearch = searchTerm;
        ViewBag.CurrentCustomer = customerId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View(paginatedSales);
    }

    // GET: Sales/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await GetCustomersSelectList();
        ViewBag.Products = await GetProductsForSale(); // ✅ الآن تُرجع List<ProductForSaleDto>
        return View();
    }

    // POST: Sales/Create
    [HttpPost] 
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "البيانات غير صحيحة" });
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { error = "الرجاء إضافة منتجات للفاتورة" });
        }

            // ✅ التحقق من المخزون باستخدام InventoryBatch
        foreach (var itemDto in dto.Items)
        {
            var totalAvailable = await _context.InventoryBatches
                .Where(b => b.ProductId == itemDto.ProductId)
                .SumAsync(b => b.RemainingQuantity);

            if (totalAvailable < itemDto.Quantity)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                return BadRequest(new { error = $"الكمية المتاحة من {product?.Name ?? "المنتج"} هي {totalAvailable} فقط" });
            }
        }

        if (dto.paymentType == PaymentMethod.Installment)
        {
            if (dto.Installment == null || dto.Installment.NumberOfMonths <= 0)
            {
                return BadRequest(new { error = "الرجاء إدخال تفاصيل التقسيط" });
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create sale
            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                SaleDate = dto.SaleDate,
                TotalAmount = dto.TotalAmount,
                PaidAmount = dto.PaidAmount,
                RemainingAmount = dto.RemainingAmount,
                PaymentMethod = dto.paymentType,
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // ✅ Add sale items and deduct from inventory batches (FIFO)
            foreach (var itemDto in dto.Items)
            {
                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = itemDto.Total
                };

                _context.SaleItems.Add(saleItem);

                // ✅ خصم من InventoryBatches باستخدام FIFO
                await DeductFromInventoryBatches(itemDto.ProductId, itemDto.Quantity);

                // ✅ تحديث Product Status إذا نفذ المخزون
                await UpdateProductStatus(itemDto.ProductId);
            }

            await _context.SaveChangesAsync();

            // إنشاء خطة التقسيط (نفس الكود القديم)
            if (dto.paymentType == PaymentMethod.Installment && dto.Installment != null)
            {
                var principalAmount = dto.TotalAmount - dto.Installment.DownPayment;
                var interestAmount = principalAmount * (dto.Installment.InterestRate / 100) * dto.Installment.NumberOfMonths;
                var totalWithInterest = principalAmount + interestAmount;
                var monthlyPayment = totalWithInterest / dto.Installment.NumberOfMonths;

                var installmentPlan = new InstallmentPlan
                {
                    SaleId = sale.Id,
                    NumberOfMonths = dto.Installment.NumberOfMonths,
                    InterestRate = dto.Installment.InterestRate,
                    TotalWithInterest = dto.TotalAmount + interestAmount,
                    MonthlyPaymentAmount = monthlyPayment,
                    DownPayment = dto.Installment.DownPayment,
                    RemainingAmount = totalWithInterest,
                    StartDate = dto.SaleDate,
                    Status = InstallmentStatus.Active
                };

                _context.InstallmentPlans.Add(installmentPlan);
                await _context.SaveChangesAsync();

                var payments = new List<InstallmentPayment>();
                for (int i = 1; i <= dto.Installment.NumberOfMonths; i++)
                {
                    payments.Add(new InstallmentPayment
                    {
                        InstallmentPlanId = installmentPlan.Id,
                        PaymentNumber = i,
                        DueDate = dto.SaleDate.AddMonths(i),
                        AmountDue = monthlyPayment,
                        AmountPaid = 0,
                        Status = PaymentStatus.Pending
                    });
                }

                _context.InstallmentPayments.AddRange(payments);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return Ok(new { success = true, message = "تم إنشاء الفاتورة بنجاح", saleId = sale.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    // ✅ دالة خصم من المخزون (FIFO)
    private async Task DeductFromInventoryBatches(int productId, int quantity)
    {
        var remainingToDeduct = quantity;

        // ✅ FIFO: خصم من أقدم دفعة أولاً
        var batches = await _context.InventoryBatches
            .Where(b => b.ProductId == productId && b.RemainingQuantity > 0)
            .OrderBy(b => b.PurchaseDate) // أقدم دفعة أولاً
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remainingToDeduct <= 0) break;

            var deductFromBatch = Math.Min(batch.RemainingQuantity, remainingToDeduct);
            batch.RemainingQuantity -= deductFromBatch;
            remainingToDeduct -= deductFromBatch;
        }
    }

    // ✅ تحديث حالة المنتج
    private async Task UpdateProductStatus(int productId)
    {
        var totalRemaining = await _context.InventoryBatches
            .Where(b => b.ProductId == productId)
            .SumAsync(b => b.RemainingQuantity);

        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            // ✅ تغيير Status فقط إذا نفذ المخزون
            product.Status = totalRemaining > 0 ? ProductStatus.New : ProductStatus.Sold;
        }
    }

    // GET: Sales/Details/5
    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        var dto = new SaleDetailsDto
        {
            Id = sale.Id,
            SaleDate = sale.SaleDate,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer.Name,
            CustomerPhone = sale.Customer.Phone,
            TotalAmount = sale.TotalAmount,
            PaidAmount = sale.PaidAmount,
            RemainingAmount = sale.RemainingAmount,
            PaymentType = sale.PaymentMethod,
            Items = sale.SaleItems.Select(i => new SaleItemDetailsDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.TotalPrice
            }).ToList()
        };

        return Ok(dto);
    }

    // POST: Sales/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null)
            return NotFound(new { error = "الفاتورة غير موجودة" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // ✅ إرجاع الكميات إلى المخزون
            foreach (var item in sale.SaleItems)
            {
                await ReturnToInventoryBatches(item.ProductId, item.Quantity);
                await UpdateProductStatus(item.ProductId);
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "تم حذف الفاتورة بنجاح" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    // ✅ دالة إرجاع للمخزون (LIFO)
    private async Task ReturnToInventoryBatches(int productId, int quantity)
    {
        var remainingToReturn = quantity;

        // ✅ LIFO للإرجاع: إرجاع لأحدث دفعة
        var batches = await _context.InventoryBatches
            .Where(b => b.ProductId == productId && b.RemainingQuantity < b.Quantity)
            .OrderByDescending(b => b.PurchaseDate) // أحدث دفعة أولاً
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remainingToReturn <= 0) break;

            var maxCanReturn = batch.Quantity - batch.RemainingQuantity;
            var returnToBatch = Math.Min(maxCanReturn, remainingToReturn);
            
            batch.RemainingQuantity += returnToBatch;
            remainingToReturn -= returnToBatch;
        }
    }

    // GET: Sales/GetAvailableProducts
    [HttpGet]
    public async Task<IActionResult> GetAvailableProducts(string search = "")
    {
        // ✅ المنتجات التي لديها مخزون متاح
        var productsWithStock = await _context.InventoryBatches
            .Where(b => b.RemainingQuantity > 0)
            .Select(b => b.ProductId)
            .Distinct()
            .ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => productsWithStock.Contains(p.Id));

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Barcode.Contains(search));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Take(50)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                barcode = p.Barcode,
                category = p.Category.Name,
                price = p.SalePrice,
                imagePath = p.ImagePath,
                // ✅ إضافة الكمية المتاحة
                availableQuantity = _context.InventoryBatches
                    .Where(b => b.ProductId == p.Id)
                    .Sum(b => b.RemainingQuantity)
            })
            .ToListAsync();

        return Ok(products);
    }

    // ✅ GET: Sales/GetStats
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var totalSales = await _context.Sales.CountAsync();
        var totalAmount = await _context.Sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
        var todaySales = await _context.Sales
            .Where(s => s.SaleDate.Date == DateTime.Today)
            .CountAsync();
        var cashSales = await _context.Sales
            .Where(s => s.PaymentMethod == PaymentMethod.Cash)
            .CountAsync();
        var installmentSales = await _context.Sales
            .Where(s => s.PaymentMethod == PaymentMethod.Installment)
            .CountAsync();

        return Json(new
        {
            totalSales,
            totalAmount,
            todaySales,
            cashSales,
            installmentSales
        });
    }

    // ✅ GET: Sales/GetRecent
    [HttpGet]
    public async Task<IActionResult> GetRecent(int count = 5)
    {
        var sales = await _context.Sales
            .Include(s => s.Customer)
            .OrderByDescending(s => s.SaleDate)
            .Take(count)
            .Select(s => new
            {
                s.Id,
                s.SaleDate,
                CustomerName = s.Customer.Name,
                s.TotalAmount,
                s.PaymentMethod
            })
            .ToListAsync();

        return Json(sales);
    }

    // ✅ Private helper methods
    private async Task<List<SelectListItem>> GetCustomersSelectList()
    {
        return await _context.Customers
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} - {c.Phone}"
            })
            .ToListAsync();
    }

    private async Task<List<ProductForSaleDto>> GetProductsForSale()
    {
        var productsWithStock = await _context.InventoryBatches
            .Where(b => b.RemainingQuantity > 0)
            .Select(b => b.ProductId)
            .Distinct()
            .ToListAsync();

        return await _context.Products
            .Include(p => p.Category)
            .Where(p => productsWithStock.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new ProductForSaleDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Category = p.Category.Name,
                Price = p.SalePrice,
                ImagePath = p.ImagePath
            })
            .ToListAsync();
    }
}