using Application.Common.Model;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using POS.Application.DTOs;

namespace Application.Services
{
    public interface ISupplierService
    {
        Task<Result<IEnumerable<Supplier>>> GetAllAsync();
        Task<Result<Supplier>> GetByIdAsync(int id);

        Task<Result> AddAsync(CreateSupplierDto dto);
        Task<Result> UpdateAsync(UpdateSupplierDto dto);
        Task<Result> DeleteAsync(int id);
    }
}

