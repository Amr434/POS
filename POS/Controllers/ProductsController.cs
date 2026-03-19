using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using Domain.Entities;
using Domain.Enums;

namespace POS.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.InventoryBatches)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Calculate current stock for each product using InventoryBatch
            var productViewModels = products.Select(p => new {
                p.Id,
                p.Name,
                p.Barcode,
                p.SalePrice,
                CurrentStock = p.InventoryBatches?.Sum(b => b.RemainingQuantity) ?? 0,
                p.MinStock,
                p.Status,
                CategoryName = p.Category?.Name
            }).ToList();

            return View(productViewModels);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateProductVm
            {
                Categories = await GetCategoriesSelectList(),
                Status = ProductStatus.New,  // Auto-default to "New"
                MinStock = 5,                 // Smart default
                SalePrice = 0
            };

            return View(viewModel);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductVm model, string action)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Smart default for MinStock if not provided
                    if (model.MinStock == 0)
                    {
                        model.MinStock = 5;
                    }

                    var product = new Product
                    {
                        Name = model.Name,
                        CategoryId = model.CategoryId,
                        SalePrice = model.SalePrice,
                        MinStock = model.MinStock,
                        Barcode = model.Barcode,
                        Status = model.Status,
                        EngineNumber = model.IsMotorcycle ? model.EngineNumber : null,
                        ChassisNumber = model.IsMotorcycle ? model.ChassisNumber : null
                    };

                    // Handle image upload
                    if (model.Image != null && model.Image.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.Image.CopyToAsync(fileStream);
                        }
                        product.ImagePath = $"/images/products/{uniqueFileName}";

                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم إضافة المنتج بنجاح ✓";

                    // Check which button was clicked
                    if (action == "saveAndAddAnother")
                    {
                        return RedirectToAction(nameof(Create));
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"حدث خطأ أثناء حفظ المنتج: {ex.Message}");
                }
            }

          
            // If we got here, something failed, redisplay form
            model.Categories = await GetCategoriesSelectList();
            return View(model);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            var viewModel = new CreateProductVm
            {
                Name = product.Name,
                CategoryId = product.CategoryId,
                SalePrice = product.SalePrice,
                MinStock = product.MinStock,
                Barcode = product.Barcode,
                Status = product.Status,
                IsMotorcycle = !string.IsNullOrEmpty(product.EngineNumber) || !string.IsNullOrEmpty(product.ChassisNumber),
                EngineNumber = product.EngineNumber,
                ChassisNumber = product.ChassisNumber,
                Categories = await GetCategoriesSelectList()
            };

            // Optionally, you can show current stock in the edit view by summing InventoryBatches
            ViewBag.CurrentStock = await _context.InventoryBatches.Where(b => b.ProductId == product.Id).SumAsync(b => b.RemainingQuantity);
            return View(viewModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProductVm model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return NotFound();

                    product.Name = model.Name;
                    product.CategoryId = model.CategoryId;
                    product.SalePrice = model.SalePrice;
                    product.MinStock = model.MinStock;
                    product.Barcode = model.Barcode;
                    product.Status = model.Status;
                    product.EngineNumber = model.IsMotorcycle ? model.EngineNumber : null;
                    product.ChassisNumber = model.IsMotorcycle ? model.ChassisNumber : null;

                    // Handle image upload if provided
                    if (model.Image != null && model.Image.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}_{model.Image.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.Image.CopyToAsync(fileStream);
                        }
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم تحديث المنتج بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProductExists(id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"حدث خطأ أثناء تحديث المنتج: {ex.Message}");
                }
            }

            model.Categories = await GetCategoriesSelectList();
            return View(model);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم حذف المنتج بنجاح";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }

        private async Task<List<SelectListItem>> GetCategoriesSelectList()
        {
            return await _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}