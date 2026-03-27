using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(c => c.Phone)
               .HasMaxLength(50);

        builder.Property(c => c.Email)
               .HasMaxLength(200);

        builder.Property(c => c.Address)
               .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
               .IsRequired();

        // Indexes
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.Email);

        // Relationships
        builder.HasMany(c => c.Sales)
               .WithOne(s => s.Customer)
               .HasForeignKey(s => s.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}