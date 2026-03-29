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

        // التحقق من وجود منتجات
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { error = "الرجاء إضافة منتجات للفاتورة" });
        }

        // التحقق من التقسيط
        if (dto.PaymentMethod == PaymentMethod.Installment)
        {
            if (dto.Installment == null || dto.Installment.NumberOfMonths <= 0)
            {
                return BadRequest(new { error = "الرجاء إدخال تفاصيل التقسيط" });
            }

            if (dto.Installment.DownPayment < dto.TotalAmount * 0.10m)
            {
                return BadRequest(new { error = "الدفعة المقدمة يجب ألا تقل عن 10% من الإجمالي" });
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
                PaymentMethod = dto.PaymentMethod,
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Add sale items and update product status
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

                // Update product status to Sold
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product != null)
                {
                    product.Status = ProductStatus.Sold;
                }
            }

            await _context.SaveChangesAsync();

            // ✅ إنشاء خطة التقسيط إذا كانت طريقة الدفع تقسيط
            if (dto.PaymentMethod == PaymentMethod.Installment && dto.Installment != null)
            {
                // حساب الفائدة
                var principalAmount = dto.TotalAmount - dto.Installment.DownPayment;
                var interestAmount = principalAmount * (dto.Installment.InterestRate / 100) * dto.Installment.NumberOfMonths;
                var totalWithInterest = principalAmount + interestAmount;
                var monthlyPayment = totalWithInterest / dto.Installment.NumberOfMonths;

                // إنشاء خطة التقسيط
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

                // ✅ إنشاء الأقساط الشهرية
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

    // ✅ إضافة method لحساب نسبة الفائدة حسب عدد الأشهر
    private decimal GetInterestRate(int months)
    {
        return months switch
        {
            3 => 1.5m,   // 1.5% شهرياً
            6 => 2.0m,   // 2% شهرياً
            12 => 2.5m,  // 2.5% شهرياً
            24 => 3.0m,  // 3% شهرياً
            _ => 2.0m    // افتراضي
        };
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
            // Update products back to New status
            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null && product.Status == ProductStatus.Sold)
                {
                    product.Status = ProductStatus.New;
                }
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

    // GET: Sales/GetAvailableProducts - For AJAX
    [HttpGet]
    public async Task<IActionResult> GetAvailableProducts(string search = "")
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.New);

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
                imagePath = p.ImagePath
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: Sales/GetStats
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

    // GET: Sales/GetRecent
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
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.New)
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