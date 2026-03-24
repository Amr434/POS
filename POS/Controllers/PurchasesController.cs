using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using POS.Models;

namespace POS.Controllers
{
    public class PurchasesController : Controller
    {
        private readonly AppDbContext _context;

        public PurchasesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
            return View(purchases);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PurchaseFormVm();
            await PopulateLookupsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(PurchaseFormVm model)
        {
            NormalizeItems(model);
            ValidateItems(model);

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(model);
                return View("Create", model);
            }

            var purchase = new Purchase
            {
                SupplierId = model.SupplierId,
                PurchaseDate = model.PurchaseDate,
                Status = PurchaseStatus.Draft,
                TotalAmount = model.Total,
                Items = model.Items.Select(i => new PurchaseItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حفظ المسودة بنجاح";
            return RedirectToAction(nameof(Edit), new { id = purchase.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
            {
                return NotFound();
            }

            if (purchase.Status == PurchaseStatus.Completed)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل شراء مكتمل";
                return RedirectToAction(nameof(Receipt), new { id });
            }

            var model = new PurchaseFormVm
            {
                Id = purchase.Id,
                SupplierId = purchase.SupplierId,
                PurchaseDate = purchase.PurchaseDate,
                Status = purchase.Status,
                Items = purchase.Items
                    .Select(i => new PurchaseItemInputVm
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList()
            };

            if (!model.Items.Any())
            {
                model.Items.Add(new PurchaseItemInputVm());
            }

            await PopulateLookupsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseFormVm model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
            {
                return NotFound();
            }

            if (purchase.Status == PurchaseStatus.Completed)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل شراء مكتمل";
                return RedirectToAction(nameof(Receipt), new { id });
            }

            NormalizeItems(model);
            ValidateItems(model);

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(model);
                return View(model);
            }

            purchase.SupplierId = model.SupplierId;
            purchase.PurchaseDate = model.PurchaseDate;
            purchase.TotalAmount = model.Total;

            _context.PurchaseItems.RemoveRange(purchase.Items);
            purchase.Items = model.Items.Select(i => new PurchaseItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث المسودة بنجاح";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var purchase = await _context.Purchases.FirstOrDefaultAsync(p => p.Id == id);
            if (purchase == null)
            {
                return NotFound();
            }

            if (purchase.Status == PurchaseStatus.Completed)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف شراء مكتمل";
                return RedirectToAction(nameof(Receipt), new { id });
            }

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف المسودة";
            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
            {
                return NotFound();
            }

            if (purchase.Status == PurchaseStatus.Completed)
            {
                return RedirectToAction(nameof(Receipt), new { id });
            }

            if (!purchase.Items.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن إكمال شراء بدون منتجات";
                return RedirectToAction(nameof(Edit), new { id });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            purchase.TotalAmount = purchase.Items.Sum(i => i.Quantity * i.UnitPrice);
            purchase.Status = PurchaseStatus.Completed;

            foreach (var item in purchase.Items)
            {
                _context.InventoryBatches.Add(new InventoryBatch
                {
                    ProductId = item.ProductId,
                    PurchaseItemId = item.Id,
                    Quantity = item.Quantity,
                    RemainingQuantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    PurchaseDate = purchase.PurchaseDate
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "تم إكمال الشراء وتحديث المخزون";
            return RedirectToAction(nameof(Receipt), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
            {
                return NotFound();
            }

            if (purchase.Status != PurchaseStatus.Completed)
            {
                TempData["ErrorMessage"] = "الفاتورة متاحة فقط بعد إكمال الشراء";
                return RedirectToAction(nameof(Edit), new { id });
            }

            return View(purchase);
        }

        private static void NormalizeItems(PurchaseFormVm model)
        {
            model.Items = model.Items
                .Where(i => i.ProductId > 0 || i.Quantity > 0 || i.UnitPrice > 0)
                .ToList();
        }

        private void ValidateItems(PurchaseFormVm model)
        {
            if (model.Items.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Items), "يجب إضافة منتج واحد على الأقل");
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.ProductId <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].ProductId", "اختر منتج");
                }

                if (item.Quantity <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "الكمية يجب أن تكون أكبر من صفر");
                }

                if (item.UnitPrice < 0)
                {
                    ModelState.AddModelError($"Items[{i}].UnitPrice", "السعر غير صحيح");
                }
            }
        }

        private async Task PopulateLookupsAsync(PurchaseFormVm model)
        {
            model.Suppliers = await _context.Suppliers
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            model.Products = await _context.Products
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync();
        }
    }
}
