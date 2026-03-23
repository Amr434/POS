using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POS.Application.Interfaces.Repositories
{
    public interface ISupplierRepository
    {
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);

        Task AddAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(Supplier supplier);

        Task<bool> HasPurchasesAsync(int supplierId);
    }
}

