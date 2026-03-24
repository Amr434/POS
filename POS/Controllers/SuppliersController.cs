using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using POS.Application.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace POS.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _supplierService.GetAllAsync();
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Error;
                return View(new List<Supplier>());
            }

            return View(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _supplierService.AddAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم إضافة المورد بنجاح" });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateSupplierDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _supplierService.UpdateAsync(dto);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم تحديث المورد بنجاح" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _supplierService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "تم حذف المورد بنجاح" });
        }
    }
}

