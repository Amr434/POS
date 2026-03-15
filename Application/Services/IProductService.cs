using Application.Common.Model;
using POS.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace POS.Application.Services
{
    public interface IProductService
    {
        Task<Result<IEnumerable<ProductDto>>> GetAllAsync();
        Task<Result<ProductDto>> GetByIdAsync(int id);
        Task<Result<IEnumerable<ProductDto>>> GetLowStockAsync();
        Task<Result<IEnumerable<ProductDto>>> SearchAsync(string term);
        Task<Result> AddAsync(CreateProductDto dto);
        Task<Result> UpdateAsync(UpdateProductDto dto);
        Task<Result> DeleteAsync(int id);
    }
}