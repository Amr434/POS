using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using Domain.Entities;
using Domain.Enums;
using POS.Models;
using POS.Application.Services;

namespace POS.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(IProductService productService, IWebHostEnvironment environment)
        {
            _productService = productService;
            _environment = environment;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAsync();
               

            // Apply filters
            IndexProductVm indexProductVm=new IndexProductVm();
            indexProductVm.Products = products.Value;
            indexProductVm.Categories = await _context.Categories.ToListAsync();

            return View(indexProductVm);
        }

        // GET: Products/GetDetails/5 - For Preview Modal
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
                return NotFound(new { error = "المنتج غير موجود" });

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                categoryName = product.Category?.Name ?? "غير مصنف",
                salePrice = product.SalePrice,
                minStock = product.MinStock,
                barcode = product.Barcode,
                status = product.Status.ToString(),
                engineNumber = product.EngineNumber,
                chassisNumber = product.ChassisNumber,
                imagePath = product.ImagePath
            });
        }

        // GET: Products/GetForEdit/5 - For Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetForEdit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { error = "المنتج غير موجود" });

            var categories = await _context.Categories
                .Select(c => new { value = c.Id, text = c.Name })
                .ToListAsync();

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                categoryId = product.CategoryId,
                salePrice = product.SalePrice,
                minStock = product.MinStock,
                barcode = product.Barcode,
                status = product.Status,
                engineNumber = product.EngineNumber,
                chassisNumber = product.ChassisNumber,
                imagePath = product.ImagePath,
                isMotorcycle = !string.IsNullOrEmpty(product.EngineNumber) || !string.IsNullOrEmpty(product.ChassisNumber),
                categories = categories
            });
        }

        // POST: Products/UpdateQuick - For Quick Edit from Modal
        [HttpPost]
        public async Task<IActionResult> UpdateQuick([FromForm] QuickEditProductDto dto) // ✅ Use [FromForm]
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { error = "البيانات غير صالحة", details = errors });
            }

            var product = await _context.Products.FindAsync(dto.Id);
            if (product == null)
                return NotFound(new { error = "المنتج غير موجود" });

            // Update basic properties
            product.Name = dto.Name;
            product.CategoryId = dto.CategoryId;
            product.SalePrice = dto.SalePrice;
            product.MinStock = dto.MinStock;
            product.Barcode = dto.Barcode;
            product.Status = dto.Status;

            // Handle motorcycle fields
            if (dto.IsMotorcycle)
            {
                product.EngineNumber = dto.EngineNumber;
                product.ChassisNumber = dto.ChassisNumber;
            }
            else
            {
                product.EngineNumber = null;
                product.ChassisNumber = null;
            }

            // Handle image upload
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{dto.Image.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                product.ImagePath = $"/images/products/{uniqueFileName}";
            }

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                return Ok(new { message = "تم تحديث المنتج بنجاح" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"حدث خطأ أثناء التحديث: {ex.Message}" });
            }
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateProductVm
            {
                Categories = await GetCategoriesSelectList(),
                Status = ProductStatus.New,  // Auto-default to "New"
                MinStock =1,                 // Smart default
             
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

    // DTO for Quick Edit
}