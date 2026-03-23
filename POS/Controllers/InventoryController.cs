using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using POS.Models;

namespace POS.Controllers
{
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(int pageNumber = 1, string searchTerm = "", int? categoryId = null, string stockFilter = "")
        {
            var query = BuildInventoryQuery(searchTerm, categoryId, stockFilter);

            var paginatedList = await PaginatedList<InventoryIndexVm>.CreateAsync(query, pageNumber, PageSize);

            // Summary stats (unfiltered)
            var allProducts = _context.Products
                .Include(p => p.InventoryBatches)
                .AsQueryable();

            var allItems = await allProducts.Select(p => new
            {
                TotalStock = p.InventoryBatches.Sum(b => b.RemainingQuantity),
                p.MinStock
            }).ToListAsync();

            ViewBag.TotalProducts = allItems.Count;
            ViewBag.TotalStockUnits = allItems.Sum(x => x.TotalStock);
            ViewBag.LowStockCount = allItems.Count(x => x.TotalStock > 0 && x.TotalStock <= x.MinStock);
            ViewBag.OutOfStockCount = allItems.Count(x => x.TotalStock <= 0);

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentStockFilter = stockFilter;

            return View(paginatedList);
        }

        // GET: Inventory/GetPage - AJAX endpoint for pagination
        [HttpGet]
        public async Task<IActionResult> GetPage(int pageNumber = 1, string searchTerm = "", int? categoryId = null, string stockFilter = "")
        {
            var query = BuildInventoryQuery(searchTerm, categoryId, stockFilter);
            var paginatedList = await PaginatedList<InventoryIndexVm>.CreateAsync(query, pageNumber, PageSize);

            var result = new
            {
                items = paginatedList.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.CategoryName,
                    i.Barcode,
                    i.ImagePath,
                    i.TotalStock,
                    i.MinStock,
                    i.StockStatus,
                    stockStatusText = InventoryIndexVm.GetStockStatusText(i.StockStatus)
                }),
                pageIndex = paginatedList.PageIndex,
                totalPages = paginatedList.TotalPages,
                totalCount = paginatedList.TotalCount,
                hasNextPage = paginatedList.HasNextPage,
                hasPreviousPage = paginatedList.HasPreviousPage
            };

            return Ok(result);
        }

        // GET: Inventory/GetDetails/5 - Batch details for a product
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { error = "المنتج غير موجود" });

            var batches = await _context.InventoryBatches
                .Where(b => b.ProductId == id && b.RemainingQuantity > 0)
                .OrderByDescending(b => b.PurchaseDate)
                .Select(b => new
                {
                    b.Id,
                    b.Quantity,
                    b.RemainingQuantity,
                    b.UnitPrice,
                    purchaseDate = b.PurchaseDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            var totalStock = batches.Sum(b => b.RemainingQuantity);

            return Ok(new
            {
                productId = product.Id,
                productName = product.Name,
                categoryName = product.Category?.Name ?? "غير مصنف",
                barcode = product.Barcode,
                imagePath = product.ImagePath,
                minStock = product.MinStock,
                totalStock,
                stockStatus = InventoryIndexVm.GetStockStatus(totalStock, product.MinStock),
                batches
            });
        }

        // POST: Inventory/AdjustStock - Manual stock adjustment
        [HttpPost]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "بيانات غير صالحة" });

            var batch = await _context.InventoryBatches.FindAsync(request.BatchId);
            if (batch == null)
                return NotFound(new { error = "الدفعة غير موجودة" });

            if (request.AdjustmentType == "remove")
            {
                if (request.Quantity > batch.RemainingQuantity)
                    return BadRequest(new { error = "الكمية المطلوبة أكبر من المتبقي" });

                batch.RemainingQuantity -= request.Quantity;
            }
            else // add
            {
                batch.RemainingQuantity += request.Quantity;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "تم تعديل المخزون بنجاح" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"حدث خطأ: {ex.Message}" });
            }
        }

        private IQueryable<InventoryIndexVm> BuildInventoryQuery(string searchTerm, int? categoryId, string stockFilter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    p.Barcode.Contains(searchTerm));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var projected = query.Select(p => new InventoryIndexVm
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : "غير مصنف",
                Barcode = p.Barcode,
                ImagePath = p.ImagePath,
                TotalStock = p.InventoryBatches.Sum(b => b.RemainingQuantity),
                MinStock = p.MinStock,
                StockStatus = p.InventoryBatches.Sum(b => b.RemainingQuantity) <= 0 ? "out"
                            : p.InventoryBatches.Sum(b => b.RemainingQuantity) <= p.MinStock ? "low"
                            : "normal"
            });

            if (!string.IsNullOrEmpty(stockFilter))
            {
                projected = stockFilter switch
                {
                    "low" => projected.Where(x => x.StockStatus == "low"),
                    "out" => projected.Where(x => x.StockStatus == "out"),
                    _ => projected
                };
            }

            return projected.OrderBy(x => x.StockStatus == "out" ? 0 : x.StockStatus == "low" ? 1 : 2)
                           .ThenBy(x => x.ProductName);
        }
    }

    public class StockAdjustmentRequest
    {
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public string AdjustmentType { get; set; } // "add" or "remove"
        public string? Reason { get; set; }
    }
}
