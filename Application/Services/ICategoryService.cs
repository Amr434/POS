using Application.Common.Model;
using Domain.Entities;
using POS.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Application.Services
{
    public interface ICategoryService
    {
        Task<Result<IEnumerable<Category>>> GetAllAsync();
        Task<Result<CategoryDto>> GetByIdAsync(int id);
        Task<Result<IEnumerable<CategoryDto>>> GetActiveCategoriesAsync();
        Task<Result<CategoryDto>> GetByNameAsync(string name);
        Task<Result> AddAsync(CreateCategoryDto dto);
        Task<Result> UpdateAsync(UpdateCategoryDto dto);
        Task<Result> DeleteAsync(int id);
        Task<Result<int>> GetProductCountAsync(int categoryId);
    }
}
