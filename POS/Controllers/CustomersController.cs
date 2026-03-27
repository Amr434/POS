using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;

namespace POS.Controllers;

public class CustomersController : Controller
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _context.Customers
            .Include(c => c.Sales)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(customers);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { error = "اسم العميل مطلوب" });

            var customer = new Customer
            {
                Name = dto.Name.Trim(),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim(),
                Email = dto.Email?.Trim(),
                NationalId=dto.NationalId

            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم إضافة العميل بنجاح", customerId = customer.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] CustomerDto dto)
    {
        try
        {
            if (dto.Id == 0)
                return BadRequest(new { error = "معرف العميل مطلوب" });

            var customer = await _context.Customers.FindAsync(dto.Id);
            if (customer == null)
                return NotFound(new { error = "العميل غير موجود" });

            customer.Name = dto.Name.Trim();
            customer.Phone = dto.Phone?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.Email = dto.Email?.Trim();

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم تحديث بيانات العميل بنجاح" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Sales)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound(new { error = "العميل غير موجود" });

            if (customer.Sales.Any())
                return BadRequest(new { error = "لا يمكن حذف العميل لوجود فواتير مرتبطة به" });

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حذف العميل بنجاح" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerSales(int customerId)
    {
        var sales = await _context.Sales
            .Where(s => s.CustomerId == customerId)
            .Include(s => s.SaleItems)
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new
            {
                s.Id,
                s.SaleDate,
                s.TotalAmount,
                s.PaymentMethod,
                ItemsCount = s.SaleItems.Count
            })
            .ToListAsync();

        return Json(sales);
    }
}

