using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using POS.Application.Interfaces.Repositories;
using Domain.Entities;
using POS.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace POS.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Category category)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories.Include(x=>x.Products).ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id, bool withProducts=false)
        {
            if (withProducts)
            {
                return await _context.Categories.Include(x => x.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            else
            {
                return await _context.Categories
                                    .FirstOrDefaultAsync(x => x.Id == id);
            }
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}