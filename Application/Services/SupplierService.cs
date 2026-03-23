using Application.Common.Model;
using Domain.Entities;
using POS.Application.DTOs;
using POS.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<Result<IEnumerable<Supplier>>> GetAllAsync()
        {
            var suppliers = await _supplierRepository.GetAllAsync();
            return Result<IEnumerable<Supplier>>.SuccessResult(suppliers);
        }

        public async Task<Result<Supplier>> GetByIdAsync(int id)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier == null)
                return Result<Supplier>.Failure("المورد غير موجود");

            return Result<Supplier>.SuccessResult(supplier);
        }

        public async Task<Result> AddAsync(CreateSupplierDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result.Failure("اسم المورد مطلوب");

            var supplier = new Supplier
            {
                Name = dto.Name.Trim(),
                Phone = dto.Phone?.Trim(),
                Address = dto.Address?.Trim()
            };

            await _supplierRepository.AddAsync(supplier);
            return Result.SuccessResult();
        }

        public async Task<Result> UpdateAsync(UpdateSupplierDto dto)
        {
            var supplier = await _supplierRepository.GetByIdAsync(dto.Id);
            if (supplier == null)
                return Result.Failure("المورد غير موجود");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result.Failure("اسم المورد مطلوب");

            supplier.Name = dto.Name.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Address = dto.Address?.Trim();

            await _supplierRepository.UpdateAsync(supplier);
            return Result.SuccessResult();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id);
            if (supplier == null)
                return Result.Failure("المورد غير موجود");

            if (await _supplierRepository.HasPurchasesAsync(id))
                return Result.Failure("لا يمكن حذف المورد لأنه مستخدم في عمليات شراء");

            await _supplierRepository.DeleteAsync(supplier);
            return Result.SuccessResult();
        }
    }
}

