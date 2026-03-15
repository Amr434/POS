using Application.Common.Model;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using POS.Application.DTOs;
using POS.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ICategoryRepository _catRepo;

        public ProductService(IProductRepository repo, ICategoryRepository catRepo)
        {
            _repo = repo;
            _catRepo = catRepo;
        }

        public async Task<Result> AddAsync(CreateProductDto dto)
        {
            var category = await _catRepo.GetByIdAsync(dto.CategoryId);
            if (category == null)
                return Result.Failure("الفئة غير موجودة"); // Arabic error

            var product = new Product
            {
                Name = dto.Name,
                Barcode = dto.Barcode,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                Quantity = dto.Quantity,
                MinStock = dto.MinStock,
                CategoryId = dto.CategoryId,
                Status = (ProductStatus)dto.Status
            };

            await _repo.AddAsync(product);
            return Result.SuccessResult();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                return Result.Failure("المنتج غير موجود");

            await _repo.DeleteAsync(product);
            return Result.SuccessResult();
        }

        public async Task<Result<IEnumerable<ProductDto>>> GetAllAsync()
        {
            var products = await _repo.GetAllAsync();
            var dtoList = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Quantity = p.Quantity,
                Status = p.Status.ToString(),
                CategoryName = p.Category?.Name
            });
            return Result<IEnumerable<ProductDto>>.SuccessResult(dtoList);
        }

        public async Task<Result<ProductDto>> GetByIdAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                return Result<ProductDto>.Failure("المنتج غير موجود");

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Barcode = product.Barcode,
                Quantity = product.Quantity,
                Status = product.Status.ToString(),
                CategoryName = product.Category?.Name
            };

            return Result<ProductDto>.SuccessResult(dto);
        }

        public async Task<Result<IEnumerable<ProductDto>>> GetLowStockAsync()
        {
            var products = await _repo.GetLowStockAsync();
            var dtoList = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Quantity = p.Quantity,
                MinStock = p.MinStock,
                Status = p.Status.ToString(),
                CategoryName = p.Category?.Name
            });

            return Result<IEnumerable<ProductDto>>.SuccessResult(dtoList);
        }

        public async Task<Result<IEnumerable<ProductDto>>> SearchAsync(string term)
        {
            var products = await _repo.SearchAsync(term);
            var dtoList = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                Quantity = p.Quantity,
                Status = p.Status.ToString(),
                CategoryName = p.Category?.Name
            });

            return Result<IEnumerable<ProductDto>>.SuccessResult(dtoList);
        }

        public async Task<Result> UpdateAsync(UpdateProductDto dto)
        {
            var product = await _repo.GetByIdAsync(dto.Id);
            if (product == null)
                return Result.Failure("المنتج غير موجود");

            var category = await _catRepo.GetByIdAsync(dto.CategoryId);
            if (category == null)
                return Result.Failure("الفئة غير موجودة");

            product.Name = dto.Name;
            product.Barcode = dto.Barcode;
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.Quantity = dto.Quantity;
            product.MinStock = dto.MinStock;
            product.CategoryId = dto.CategoryId;
            product.Status = (ProductStatus)dto.Status;

            await _repo.UpdateAsync(product);
            return Result.SuccessResult();
        }
    }
}