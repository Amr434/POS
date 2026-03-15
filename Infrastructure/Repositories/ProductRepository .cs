using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using POS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    // Infrastructure Layer
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            _context.SaveChangesAsync();
        }
          

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Products.Include(p => p.Category).ToListAsync();

        public async Task<Product> GetByIdAsync(int id) =>
            await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Product>> GetLowStockAsync()
        {
            return await _context.Products
                .Where(p => p.Quantity <= p.MinStock)
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchAsync(string term)
        {
            return await _context.Products
                .Where(p => p.Name.Contains(term) || p.Barcode.Contains(term))
                .Include(p => p.Category)
                .ToListAsync();
        }
    }
}
