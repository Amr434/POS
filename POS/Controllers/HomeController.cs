using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using POS.Models;
using POS.Infrastructure.Data;

namespace POS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaySalesQuery = _db.Sales.AsNoTracking()
                .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow);

            var todaySalesTotal = await todaySalesQuery.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            var todayInvoicesCount = await todaySalesQuery.CountAsync();

            var totalProducts = await _db.Products.AsNoTracking().CountAsync();

            var stockByProduct = await _db.InventoryBatches.AsNoTracking()
                .GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, Remaining = g.Sum(x => x.RemainingQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Remaining);

            var products = await _db.Products.AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.MinStock })
                .ToListAsync();

            var lowStockAll = products
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.MinStock,
                    Remaining = stockByProduct.TryGetValue(p.Id, out var remaining) ? remaining : 0
                })
                .Where(x => x.Remaining <= x.MinStock)
                .OrderBy(x => x.Remaining)
                .ThenBy(x => x.Name)
                .ToList();

            var recentSales = await _db.Sales.AsNoTracking()
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .Select(s => new HomeDashboardVm.RecentSaleVm
                {
                    SaleId = s.Id,
                    SaleDate = s.SaleDate,
                    CustomerName = s.Customer != null ? s.Customer.Name : "-",
                    TotalAmount = s.TotalAmount,
                    RemainingAmount = s.RemainingAmount
                })
                .ToListAsync();

            var vm = new HomeDashboardVm
            {
                Today = today,
                TodaySalesTotal = todaySalesTotal,
                TotalProducts = totalProducts,
                LowStockCount = lowStockAll.Count,
                TodayInvoicesCount = todayInvoicesCount,
                RecentSales = recentSales,
                LowStockItems = lowStockAll.Take(3).Select(x => new HomeDashboardVm.LowStockVm
                {
                    ProductId = x.Id,
                    ProductName = x.Name,
                    RemainingQuantity = x.Remaining,
                    MinStock = x.MinStock
                }).ToList()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
