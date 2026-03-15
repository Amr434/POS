using Application.Common.Model;
using Application.Interfaces;
using Domain.Entities;
using POS.Application.DTOs;
using POS.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result> AddAsync(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result.Failure("اسم التصنيف مطلوب");

            var category = new Category
            {
                Name = dto.Name
            };

            await _categoryRepository.AddAsync(category);
            return Result.SuccessResult();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id,withProducts:true );
                if (category == null)
                    return Result.Failure("التصنيف غير موجود");

                // Check if category has products
                if (category.Products != null && category.Products.Any())
                    return Result.Failure("لا يمكن حذف التصنيف لأنه يحتوي على منتجات");

                await _categoryRepository.DeleteAsync(category);
                return Result.SuccessResult();
            }
            catch
            {
                return Result.Failure("حدث خطا اثناء حذف التصنيف ");

            }

            
        }

        public async Task<Result<IEnumerable<Category>>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
       

            return Result<IEnumerable<Category>>.SuccessResult(categories);
        }

        public async Task<Result<IEnumerable<CategoryDto>>> GetActiveCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var activeCats = categories.Where(c => c.Products != null && c.Products.Any());

            var dtoList = activeCats.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = c.Products.Count
            });

            return Result<IEnumerable<CategoryDto>>.SuccessResult(dtoList);
        }

        public async Task<Result<CategoryDto>> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return Result<CategoryDto>.Failure("التصنيف غير موجود");

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ProductCount = category.Products?.Count ?? 0
            };

            return Result<CategoryDto>.SuccessResult(dto);
        }

        public async Task<Result<CategoryDto>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<CategoryDto>.Failure("اسم التصنيف مطلوب");

            var categories = await _categoryRepository.GetAllAsync();
            var category = categories.FirstOrDefault(c => c.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return Result<CategoryDto>.Failure("التصنيف غير موجود");

            var dto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ProductCount = category.Products?.Count ?? 0
            };

            return Result<CategoryDto>.SuccessResult(dto);
        }

        public async Task<Result<int>> GetProductCountAsync(int categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                return Result<int>.Failure("التصنيف غير موجود");

            var count = category.Products?.Count ?? 0;
            return Result<int>.SuccessResult(count);
        }

        public async Task<Result> UpdateAsync(UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.Id);
            if (category == null)
                return Result.Failure("التصنيف غير موجود");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result.Failure("اسم التصنيف مطلوب");

            category.Name = dto.Name;

            await _categoryRepository.UpdateAsync(category);
            return Result.SuccessResult();
        }
    }
}
