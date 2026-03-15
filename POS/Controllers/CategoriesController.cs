using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using POS.Application.DTOs;
using System.Threading.Tasks;

namespace POS.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _categoryService.GetAllAsync();
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Error;
                return View(new List<Category>());
            }

            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _categoryService.AddAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم إضافة التصنيف بنجاح" });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _categoryService.UpdateAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم تحديث التصنيف بنجاح" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم حذف التصنيف بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _categoryService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}