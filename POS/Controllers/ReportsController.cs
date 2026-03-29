using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using POS.Models.Reports;
using System.Globalization;
using System.Text;

namespace POS.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new ReportsDashboardVm();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Sales(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var query = _context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            query = ApplySaleDateRange(query, range.From, range.To);

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var rows = sales.Select(s => new SaleRowVm
            {
                Id = s.Id,
                SaleDate = s.SaleDate,
                CustomerName = s.Customer?.Name ?? "—",
                TotalAmount = s.TotalAmount,
                PaidAmount = s.PaidAmount,
                RemainingAmount = s.RemainingAmount,
                ItemsCount = s.SaleItems?.Sum(i => i.Quantity) ?? 0
            }).ToList();

            var grouped = rows
                .GroupBy(r => GetPeriodKey(r.SaleDate, groupBy))
                .OrderBy(g => g.Key)
                .Select(g => new SalesGroupVm
                {
                    Period = g.Key,
                    SalesCount = g.Count(),
                    ItemsCount = g.Sum(x => x.ItemsCount),
                    TotalAmount = g.Sum(x => x.TotalAmount),
                    PaidAmount = g.Sum(x => x.PaidAmount),
                    RemainingAmount = g.Sum(x => x.RemainingAmount)
                })
                .ToList();

            var model = new SalesReportVm
            {
                From = range.From,
                To = range.To,
                GroupBy = groupBy,
                TotalSales = rows.Count,
                TotalAmount = rows.Sum(x => x.TotalAmount),
                TotalPaid = rows.Sum(x => x.PaidAmount),
                TotalRemaining = rows.Sum(x => x.RemainingAmount),
                Rows = rows,
                Groups = grouped
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Inventory(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            // Inventory is inherently "current state"; the date range here is used only for optional purchase activity grouping.
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var items = products.Select(p =>
            {
                var totalStock = p.InventoryBatches?.Sum(b => b.RemainingQuantity) ?? 0;
                var stockStatus = totalStock <= 0 ? "out" : totalStock <= p.MinStock ? "low" : "normal";
                return new InventoryRowVm
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? "غير مصنف",
                    Barcode = p.Barcode,
                    TotalStock = totalStock,
                    MinStock = p.MinStock,
                    StockStatus = stockStatus
                };
            }).ToList();

            var purchaseBatches = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => b.PurchaseDate >= range.From && b.PurchaseDate <= range.To)
                .ToListAsync();

            var purchaseGroups = purchaseBatches
                .GroupBy(b => GetPeriodKey(b.PurchaseDate, groupBy))
                .OrderBy(g => g.Key)
                .Select(g => new InventoryPurchaseGroupVm
                {
                    Period = g.Key,
                    PurchasedUnits = g.Sum(x => x.Quantity),
                    PurchasedValue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToList();

            var model = new InventoryReportVm
            {
                From = range.From,
                To = range.To,
                GroupBy = groupBy,
                TotalProducts = items.Count,
                TotalStockUnits = items.Sum(x => x.TotalStock),
                LowStockCount = items.Count(x => x.TotalStock > 0 && x.TotalStock <= x.MinStock),
                OutOfStockCount = items.Count(x => x.TotalStock <= 0),
                Rows = items,
                PurchaseGroups = purchaseGroups
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Expenses(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= range.From && e.ExpenseDate <= range.To)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            var rows = expenses.Select(e => new ExpenseRowVm
            {
                Id = e.Id,
                ExpenseDate = e.ExpenseDate,
                Title = e.Title,
                Amount = e.Amount,
                Notes = e.Notes
            }).ToList();

            var groups = rows
                .GroupBy(r => GetPeriodKey(r.ExpenseDate, groupBy))
                .OrderBy(g => g.Key)
                .Select(g => new ExpensesGroupVm
                {
                    Period = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .ToList();

            var model = new ExpensesReportVm
            {
                From = range.From,
                To = range.To,
                GroupBy = groupBy,
                TotalAmount = rows.Sum(x => x.Amount),
                Rows = rows,
                Groups = groups
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profit(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var saleItems = await _context.SaleItems
                .AsNoTracking()
                .Include(i => i.Sale)
                .Where(i => i.Sale.SaleDate >= range.From && i.Sale.SaleDate <= range.To)
                .Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice,
                    SaleDate = i.Sale.SaleDate
                })
                .ToListAsync();

            var revenue = saleItems.Sum(i => i.Quantity * i.UnitPrice);

            // Weighted-average purchase cost per product up to `To` (inclusive)
            var avgCosts = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => b.PurchaseDate <= range.To)
                .GroupBy(b => b.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    Value = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty <= 0 ? 0m : (x.Value / x.Qty));

            var estimatedCogs = saleItems.Sum(i =>
            {
                var cost = avgCosts.TryGetValue(i.ProductId, out var c) ? c : 0m;
                return i.Quantity * cost;
            });

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= range.From && e.ExpenseDate <= range.To)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var profit = revenue - estimatedCogs - expenses;

            var groups = saleItems
                .GroupBy(i => GetPeriodKey(i.SaleDate, groupBy))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var grpRevenue = g.Sum(x => x.Quantity * x.UnitPrice);
                    var grpCogs = g.Sum(x =>
                    {
                        var cost = avgCosts.TryGetValue(x.ProductId, out var c) ? c : 0m;
                        return x.Quantity * cost;
                    });

                    return new ProfitGroupVm
                    {
                        Period = g.Key,
                        Revenue = grpRevenue,
                        EstimatedCogs = grpCogs
                    };
                })
                .ToList();

            // Attach grouped expenses to the same periods
            var expenseGroups = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= range.From && e.ExpenseDate <= range.To)
                .ToListAsync();

            var expenseByPeriod = expenseGroups
                .GroupBy(e => GetPeriodKey(e.ExpenseDate, groupBy))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            foreach (var g in groups)
            {
                g.Expenses = expenseByPeriod.TryGetValue(g.Period, out var v) ? v : 0m;
                g.Profit = g.Revenue - g.EstimatedCogs - g.Expenses;
            }

            var model = new ProfitReportVm
            {
                From = range.From,
                To = range.To,
                GroupBy = groupBy,
                Revenue = revenue,
                EstimatedCogs = estimatedCogs,
                Expenses = expenses,
                Profit = profit,
                Groups = groups
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SalesCsv(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var rows = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Where(s => s.SaleDate >= range.From && s.SaleDate <= range.To)
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new
                {
                    s.Id,
                    s.SaleDate,
                    CustomerName = s.Customer != null ? s.Customer.Name : "—",
                    ItemsCount = s.SaleItems.Sum(i => i.Quantity),
                    s.TotalAmount,
                    s.PaidAmount,
                    s.RemainingAmount
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Customer,ItemsCount,Total,Paid,Remaining");
            foreach (var r in rows)
            {
                sb.AppendLine($"{r.Id},{r.SaleDate:yyyy-MM-dd},{Csv(r.CustomerName)},{r.ItemsCount},{r.TotalAmount},{r.PaidAmount},{r.RemainingAmount}");
            }

            return CsvFile(sb.ToString(), $"sales_{range.From:yyyyMMdd}_{range.To:yyyyMMdd}_{groupBy}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> InventoryCsv(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var _ = NormalizeRange(from, to);

            var items = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : "غير مصنف",
                    p.Barcode,
                    p.MinStock,
                    TotalStock = p.InventoryBatches.Sum(b => b.RemainingQuantity)
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ProductId,ProductName,Category,Barcode,TotalStock,MinStock,StockStatus");
            foreach (var i in items)
            {
                var status = i.TotalStock <= 0 ? "out" : i.TotalStock <= i.MinStock ? "low" : "normal";
                sb.AppendLine($"{i.Id},{Csv(i.Name)},{Csv(i.CategoryName)},{Csv(i.Barcode)},{i.TotalStock},{i.MinStock},{status}");
            }

            return CsvFile(sb.ToString(), $"inventory_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExpensesCsv(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var rows = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= range.From && e.ExpenseDate <= range.To)
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new { e.Id, e.ExpenseDate, e.Title, e.Amount, e.Notes })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Title,Amount,Notes");
            foreach (var r in rows)
            {
                sb.AppendLine($"{r.Id},{r.ExpenseDate:yyyy-MM-dd},{Csv(r.Title)},{r.Amount},{Csv(r.Notes ?? string.Empty)}");
            }

            return CsvFile(sb.ToString(), $"expenses_{range.From:yyyyMMdd}_{range.To:yyyyMMdd}_{groupBy}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ProfitCsv(DateTime? from, DateTime? to, string groupBy = "day")
        {
            var range = NormalizeRange(from, to);
            groupBy = NormalizeGroupBy(groupBy);

            var saleItems = await _context.SaleItems
                .AsNoTracking()
                .Include(i => i.Sale)
                .Where(i => i.Sale.SaleDate >= range.From && i.Sale.SaleDate <= range.To)
                .Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice,
                    SaleDate = i.Sale.SaleDate
                })
                .ToListAsync();

            var avgCosts = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => b.PurchaseDate <= range.To)
                .GroupBy(b => b.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Quantity),
                    Value = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty <= 0 ? 0m : (x.Value / x.Qty));

            var expenseRows = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= range.From && e.ExpenseDate <= range.To)
                .Select(e => new { e.ExpenseDate, e.Amount })
                .ToListAsync();

            var expenseByPeriod = expenseRows
                .GroupBy(e => GetPeriodKey(e.ExpenseDate, groupBy))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var groups = saleItems
                .GroupBy(i => GetPeriodKey(i.SaleDate, groupBy))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var revenue = g.Sum(x => x.Quantity * x.UnitPrice);
                    var cogs = g.Sum(x =>
                    {
                        var cost = avgCosts.TryGetValue(x.ProductId, out var c) ? c : 0m;
                        return x.Quantity * cost;
                    });
                    var expenses = expenseByPeriod.TryGetValue(g.Key, out var v) ? v : 0m;
                    var profit = revenue - cogs - expenses;
                    return new { Period = g.Key, revenue, cogs, expenses, profit };
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Period,Revenue,EstimatedCOGS,Expenses,Profit");
            foreach (var r in groups)
            {
                sb.AppendLine($"{r.Period:yyyy-MM-dd},{r.revenue},{r.cogs},{r.expenses},{r.profit}");
            }

            return CsvFile(sb.ToString(), $"profit_{range.From:yyyyMMdd}_{range.To:yyyyMMdd}_{groupBy}.csv");
        }

        private static string NormalizeGroupBy(string? groupBy)
        {
            groupBy = (groupBy ?? "day").Trim().ToLowerInvariant();
            return groupBy is "month" ? "month" : "day";
        }

        private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
        {
            var today = DateTime.Today;
            var start = from?.Date ?? new DateTime(today.Year, today.Month, 1);
            var end = (to?.Date ?? today).Date.AddDays(1).AddTicks(-1);
            if (end < start)
            {
                (start, end) = (end.Date, start.Date.AddDays(1).AddTicks(-1));
            }
            return (start, end);
        }

        private static IQueryable<Domain.Entities.Sale> ApplySaleDateRange(IQueryable<Domain.Entities.Sale> query, DateTime from, DateTime to)
        {
            return query.Where(s => s.SaleDate >= from && s.SaleDate <= to);
        }

        private static DateTime GetPeriodKey(DateTime dt, string groupBy)
        {
            return groupBy == "month"
                ? new DateTime(dt.Year, dt.Month, 1)
                : dt.Date;
        }

        private IActionResult CsvFile(string csv, string filename)
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8.GetBytes(csv);
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

        private static string Csv(string value)
        {
            if (value == null) return string.Empty;
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            value = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{value}\"" : value;
        }
    }
}
