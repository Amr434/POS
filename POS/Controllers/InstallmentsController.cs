using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using POS.Models;

namespace POS.Controllers;

public class InstallmentsController : Controller
{
    private readonly AppDbContext _context;
    private const int PageSize = 20;

    public InstallmentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Installments/Index - عرض جميع الأقساط
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        string? searchTerm = null,
        int? customerId = null,
        PaymentStatus? status = null,
        bool showOverdueOnly = false)
    {
        // ✅ تحديث الأقساط المتأخرة أولاً
        await UpdateOverduePayments();

        var query = _context.InstallmentPayments
            .Include(p => p.InstallmentPlan)
                .ThenInclude(plan => plan.Sale)
                    .ThenInclude(s => s.Customer)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p =>
                p.InstallmentPlan.Sale.Customer.Name.Contains(searchTerm) ||
                p.InstallmentPlan.Sale.Customer.Phone.Contains(searchTerm));
        }

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(p => p.InstallmentPlan.Sale.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (showOverdueOnly)
        {
            query = query.Where(p => p.Status == PaymentStatus.Overdue);
        }

        // Order by due date
        query = query.OrderBy(p => p.DueDate);

        var paginatedPayments = await PaginatedList<InstallmentPayment>.CreateAsync(query, pageNumber, PageSize);

        // ViewBag data
        ViewBag.Customers = await GetCustomersSelectList();
        ViewBag.CurrentSearch = searchTerm;
        ViewBag.CurrentCustomer = customerId;
        ViewBag.CurrentStatus = status;
        ViewBag.ShowOverdueOnly = showOverdueOnly;

        return View(paginatedPayments);
    }

    // GET: Installments/CustomerInstallments/5 - أقساط عميل محدد
    public async Task<IActionResult> CustomerInstallments(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return NotFound();

        await UpdateOverduePayments();

        var installmentPlans = await _context.InstallmentPlans
            .Include(p => p.Sale)
            .Include(p => p.Payments)
            .Where(p => p.Sale.CustomerId == customerId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        ViewBag.Customer = customer;
        return View(installmentPlans);
    }

    // GET: Installments/PaymentDetails/5 - تفاصيل قسط
    [HttpGet]
    public async Task<IActionResult> GetPaymentDetails(int id)
    {
        var payment = await _context.InstallmentPayments
            .Include(p => p.InstallmentPlan)
                .ThenInclude(plan => plan.Sale)
                    .ThenInclude(s => s.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound(new { error = "القسط غير موجود" });

        var dto = new
        {
            payment.Id,
            payment.PaymentNumber,
            payment.DueDate,
            payment.AmountDue,
            payment.AmountPaid,
            payment.PaymentDate,
            payment.Status,
            payment.Notes,
            CustomerName = payment.InstallmentPlan.Sale.Customer.Name,
            CustomerPhone = payment.InstallmentPlan.Sale.Customer.Phone,
            SaleId = payment.InstallmentPlan.SaleId,
            TotalInstallments = payment.InstallmentPlan.NumberOfMonths,
            MonthlyAmount = payment.InstallmentPlan.MonthlyPaymentAmount,
            RemainingBalance = payment.AmountDue - payment.AmountPaid
        };

        return Ok(dto);
    }

    // POST: Installments/PayInstallment - دفع قسط
    [HttpPost]
    public async Task<IActionResult> PayInstallment([FromBody] PayInstallmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "البيانات غير صحيحة" });

        var payment = await _context.InstallmentPayments
            .Include(p => p.InstallmentPlan)
                .ThenInclude(plan => plan.Sale)
            .FirstOrDefaultAsync(p => p.Id == dto.PaymentId);

        if (payment == null)
            return NotFound(new { error = "القسط غير موجود" });

        if (dto.Amount <= 0)
            return BadRequest(new { error = "المبلغ يجب أن يكون أكبر من صفر" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var previousAmountPaid = payment.AmountPaid;
            payment.AmountPaid += dto.Amount;
            payment.PaymentDate = dto.PaymentDate ?? DateTime.Now;
            payment.Notes = dto.Notes;

            // ✅ تحديث حالة القسط
            if (payment.AmountPaid >= payment.AmountDue)
            {
                payment.Status = PaymentStatus.Paid;
                payment.AmountPaid = payment.AmountDue; // تحديد الحد الأقصى
            }
            else if (payment.AmountPaid > 0)
            {
                payment.Status = PaymentStatus.PartiallyPaid;
            }

            // ✅ تحديث الفاتورة الأصلية
            var sale = payment.InstallmentPlan.Sale;
            sale.PaidAmount += dto.Amount;
            sale.RemainingAmount -= dto.Amount;

            // ✅ تحديث خطة التقسيط
            var plan = payment.InstallmentPlan;
            plan.RemainingAmount -= dto.Amount;

            // ✅ التحقق من اكتمال كل الأقساط
            var allPayments = await _context.InstallmentPayments
                .Where(p => p.InstallmentPlanId == plan.Id)
                .ToListAsync();

            if (allPayments.All(p => p.Status == PaymentStatus.Paid))
            {
                plan.Status = InstallmentStatus.Completed;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "تم تسجيل الدفع بنجاح",
                newPaidAmount = payment.AmountPaid,
                remainingBalance = payment.AmountDue - payment.AmountPaid,
                saleRemainingAmount = sale.RemainingAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
        }
    }

    // GET: Installments/OverdueReport - تقرير المتأخرات
    public async Task<IActionResult> OverdueReport()
    {
        await UpdateOverduePayments();

        var overduePayments = await _context.InstallmentPayments
            .Include(p => p.InstallmentPlan)
                .ThenInclude(plan => plan.Sale)
                    .ThenInclude(s => s.Customer)
            .Where(p => p.Status == PaymentStatus.Overdue)
            .OrderBy(p => p.DueDate)
            .Select(p => new
            {
                p.Id,
                CustomerName = p.InstallmentPlan.Sale.Customer.Name,
                CustomerPhone = p.InstallmentPlan.Sale.Customer.Phone,
                SaleId = p.InstallmentPlan.SaleId,
                p.PaymentNumber,
                p.DueDate,
                p.AmountDue,
                p.AmountPaid,
                RemainingBalance = p.AmountDue - p.AmountPaid,
                DaysOverdue = (DateTime.Now - p.DueDate).Days
            })
            .ToListAsync();

        return View(overduePayments);
    }

    // GET: Installments/MonthlyReport - تقرير الأقساط الشهرية
    public async Task<IActionResult> MonthlyReport(int? month = null, int? year = null)
    {
        var targetMonth = month ?? DateTime.Now.Month;
        var targetYear = year ?? DateTime.Now.Year;

        var payments = await _context.InstallmentPayments
            .Include(p => p.InstallmentPlan)
                .ThenInclude(plan => plan.Sale)
                    .ThenInclude(s => s.Customer)
            .Where(p =>
                p.DueDate.Month == targetMonth &&
                p.DueDate.Year == targetYear)
            .OrderBy(p => p.DueDate)
            .ToListAsync();

        var report = new
        {
            Month = targetMonth,
            Year = targetYear,
            TotalPayments = payments.Count,
            TotalDue = payments.Sum(p => p.AmountDue),
            TotalPaid = payments.Sum(p => p.AmountPaid),
            TotalRemaining = payments.Sum(p => p.AmountDue - p.AmountPaid),
            PaidCount = payments.Count(p => p.Status == PaymentStatus.Paid),
            PendingCount = payments.Count(p => p.Status == PaymentStatus.Pending),
            OverdueCount = payments.Count(p => p.Status == PaymentStatus.Overdue),
            Payments = payments
        };

        ViewBag.Report = report;
        ViewBag.SelectedMonth = targetMonth;
        ViewBag.SelectedYear = targetYear;

        return View(report);
    }

    // GET: Installments/PlanDetails/5 - تفاصيل خطة التقسيط
    public async Task<IActionResult> PlanDetails(int id)
    {
        var plan = await _context.InstallmentPlans
            .Include(p => p.Sale)
                .ThenInclude(s => s.Customer)
            .Include(p => p.Payments.OrderBy(pay => pay.PaymentNumber))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return NotFound();

        return View(plan);
    }

    // ✅ Private Helper Methods

    // تحديث الأقساط المتأخرة تلقائياً
    private async Task UpdateOverduePayments()
    {
        var overduePayments = await _context.InstallmentPayments
            .Where(p =>
                p.Status == PaymentStatus.Pending &&
                p.DueDate < DateTime.Now.Date)
            .ToListAsync();

        foreach (var payment in overduePayments)
        {
            payment.Status = PaymentStatus.Overdue;
        }

        if (overduePayments.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    // قائمة العملاء
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

    // ✅ API: Dashboard Stats
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var totalPlans = await _context.InstallmentPlans.CountAsync();
        var activePlans = await _context.InstallmentPlans
            .CountAsync(p => p.Status == InstallmentStatus.Active);

        var totalPayments = await _context.InstallmentPayments.CountAsync();
        var paidPayments = await _context.InstallmentPayments
            .CountAsync(p => p.Status == PaymentStatus.Paid);
        var overduePayments = await _context.InstallmentPayments
            .CountAsync(p => p.Status == PaymentStatus.Overdue);

        var totalDue = await _context.InstallmentPayments
            .Where(p => p.Status != PaymentStatus.Paid)
            .SumAsync(p => p.AmountDue - p.AmountPaid);

        var thisMonthDue = await _context.InstallmentPayments
            .Where(p =>
                p.DueDate.Month == DateTime.Now.Month &&
                p.DueDate.Year == DateTime.Now.Year &&
                p.Status != PaymentStatus.Paid)
            .SumAsync(p => p.AmountDue - p.AmountPaid);

        return Json(new
        {
            totalPlans,
            activePlans,
            totalPayments,
            paidPayments,
            overduePayments,
            totalDue,
            thisMonthDue
        });
    }

    // ✅ API: Upcoming Payments (الأقساط القادمة)
    [HttpGet]
    public async Task<IActionResult> GetUpcomingPayments(int days = 7)
    {
        var endDate = DateTime.Now.AddDays(days);

        var upcomingPayments = await _context.InstallmentPayments
            .Include(p => p.InstallmentPlan.Sale.Customer)
            .Where(p =>
                p.Status == PaymentStatus.Pending &&
                p.DueDate >= DateTime.Now.Date &&
                p.DueDate <= endDate)
            .OrderBy(p => p.DueDate)
            .Select(p => new
            {
                p.Id,
                CustomerName = p.InstallmentPlan.Sale.Customer.Name,
                CustomerPhone = p.InstallmentPlan.Sale.Customer.Phone,
                p.PaymentNumber,
                p.DueDate,
                p.AmountDue,
                SaleId = p.InstallmentPlan.SaleId
            })
            .ToListAsync();

        return Json(upcomingPayments);
    }
}