using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
            builder.Property(s => s.Phone).HasMaxLength(20);
            builder.Property(s => s.Address).HasMaxLength(300);
        }
    }
}