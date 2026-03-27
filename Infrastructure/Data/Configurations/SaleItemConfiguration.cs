using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Quantity)
               .IsRequired();

        builder.Property(si => si.UnitPrice)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(si => si.DiscountPercentage)
               .HasColumnType("decimal(5,2)")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(si => si.TotalPrice)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        // Indexes
        builder.HasIndex(si => si.SaleId);
        builder.HasIndex(si => si.ProductId);

        // Relationships
        builder.HasOne(si => si.Sale)
               .WithMany(s => s.SaleItems)  // Fixed: was s.Items
               .HasForeignKey(si => si.SaleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.Product)
               .WithMany()
               .HasForeignKey(si => si.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}